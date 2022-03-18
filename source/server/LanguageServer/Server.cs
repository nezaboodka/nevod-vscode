using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Nezaboodka.Nevod.Services;
using StreamJsonRpc;

namespace Nezaboodka.Nevod.LanguageServer
{
    internal class Server
    {
        private static readonly JsonMessageFormatter s_jsonMessageFormatter;
        private JsonRpc _jsonRpc = null!; // Initialized when server starts processing
        private int _isStarted; // 1 - started, 0 - not started. Int is used to allow interlocked operations
        private bool _isInitialized;
        private bool _isShutDown;
        private int ExitCode => _isShutDown ? 0 : 1;
        private IServices _services = null!; // Initialized in Initialize rpc method. Each access should be guarded by EnsureInitializedAndRunning

        static Server()
        {
            s_jsonMessageFormatter = new JsonMessageFormatter();
            s_jsonMessageFormatter.JsonSerializer.NullValueHandling = NullValueHandling.Ignore;
            s_jsonMessageFormatter.JsonSerializer.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
            s_jsonMessageFormatter.JsonSerializer.ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
            s_jsonMessageFormatter.JsonSerializer.Converters.Add(new UriConverter());
        }

        // ProcessAsync needs to remain internal to avoid being called by rpc
        internal async Task<int> ProcessAsync(Stream sendingStream, Stream receivingStream)
        {
            EnsureNotStarted();
            _isInitialized = false;
            _isShutDown = false;
            HeaderDelimitedMessageHandler messageHandler = new(sendingStream, receivingStream, s_jsonMessageFormatter);
            _jsonRpc = new JsonRpc(messageHandler, this);
            _jsonRpc.StartListening();
            await _jsonRpc.Completion;
            return ExitCode;
        }

        [JsonRpcMethod("initialize")]
        public InitializeResult Initialize(JToken jsonParams)
        {
            EnsureStarted();
            _isInitialized = true;
            var @params = Deserialize<InitializeParams>(jsonParams);
            _services = @params.WorkspaceFolders switch
            {
                { Length: 1 } folders => new WorkspaceServices(folders[0].Uri.LocalPath),
                _ => new SingleFileServices(),
            };
            return new InitializeResult(new ServerCapabilities
            {
                TextDocumentSync = new TextDocumentSyncOptions
                {
                    OpenClose = true,
                    Change = TextDocumentSyncKind.Full
                },
                DefinitionProvider = true,
                ReferencesProvider = true,
                DocumentSymbolProvider = true,
                CodeLensProvider = new CodeLensOptions
                {
                    ResolveProvider = true
                },
                WorkspaceSymbolProvider = true,
                RenameProvider = new RenameOptions
                {
                    PrepareProvider = true
                },
                // CompletionProvider = new CompletionOptions()
                // {
                //     TriggerCharacters = new [] { "\\", "/", "." }
                // }
            });
        }

        [JsonRpcMethod("initialized")]
        public async Task InitializedAsync()
        {
            _services.PublishDiagnostics += PublishDiagnosticsAsync;
            RegistrationParams registrationParams = new(new[]
            {
                new Registration("1", "workspace/didChangeWatchedFiles")
                {
                    // Watcher is created on client.
                    RegisterOptions = new DidChangeWatchedFilesRegistrationOptions(Array.Empty<FileSystemWatcher>())
                }
            });
            await _jsonRpc.NotifyWithParameterObjectAsync("client/registerCapability", registrationParams);
        }

        [JsonRpcMethod("shutdown")]
        public void Shutdown()
        {
            EnsureInitializedAndRunning();
            _isShutDown = true;
        }

        [JsonRpcMethod("exit")]
        public void Exit()
        {
            EnsureStarted();
            Stop();
        }

        [JsonRpcMethod("textDocument/didOpen")]
        public void DocumentDidOpen(JToken jsonParams)
        {
            var @params = Deserialize<DidOpenTextDocumentParams>(jsonParams);
            _services.OpenDocument(@params.TextDocument.Uri, @params.TextDocument.Text);
        }

        [JsonRpcMethod("textDocument/didChange")]
        public void DocumentDidChange(JToken jsonParams)
        {
            var @params = Deserialize<DidChangeTextDocumentParams>(jsonParams);
            _services.UpdateDocument(@params.TextDocument.Uri, @params.ContentChanges[0].Text);
        }

        [JsonRpcMethod("textDocument/didClose")]
        public void DocumentDidClose(JToken jsonParams)
        {
            var @params = Deserialize<DidCloseTextDocumentParams>(jsonParams);
            _services.CloseDocument(@params.TextDocument.Uri);
        }

        [JsonRpcMethod("workspace/didChangeWatchedFiles")]
        public void DidChangeWatchedFiles(JToken jsonParams)
        {
            var @params = Deserialize<DidChangeWatchedFilesParams>(jsonParams);
            IEnumerable<Uri> deletedDocuments =
                @params.Changes.Where(c => c.Type is FileChangeType.Deleted).Select(c => c.Uri);
            foreach (Uri uri in deletedDocuments)
            {
                _services.DeleteDocument(uri);
            }
        }

        [JsonRpcMethod("textDocument/definition")]
        public IEnumerable<Location>? GoToDefinition(JToken jsonParams)
        {
            var @params = Deserialize<DefinitionParams>(jsonParams);
            PointerLocation location = new(@params.TextDocument.Uri, @params.Position);
            Services.Location? definition = _services.GetDefinition(location);
            if (definition is not null)
                return new Location[] { definition };
            else
            {
                IEnumerable<Services.Location>? references = _services.GetReferences(location);
                return references?.Select(r => (Location)r);
            }
        }

        [JsonRpcMethod("textDocument/references")]
        public IEnumerable<Location>? GoToReferences(JToken jsonParams)
        {
            var @params = Deserialize<ReferenceParams>(jsonParams);
            IEnumerable<Services.Location>? references = _services.GetReferences(new PointerLocation(@params.TextDocument.Uri, @params.Position));
            return references?.Select(r => (Location)r);
        }

        [JsonRpcMethod("textDocument/documentSymbol")]
        public IEnumerable<DocumentSymbol> GetDocumentSymbols(JToken jsonParams)
        {
            var @params = Deserialize<DocumentSymbolParams>(jsonParams);
            IEnumerable<Symbol> symbols = _services.GetDocumentSymbols(@params.TextDocument.Uri);
            return ConvertSymbol(symbols);
        }

        [JsonRpcMethod("textDocument/codeLens")]
        public IEnumerable<CodeLens> GetCodeLens(JToken jsonParams)
        {
            var @params = Deserialize<CodeLensParams>(jsonParams);
            return from codeLens in _services.GetCodeLens(@params.TextDocument.Uri)
                let data = Serialize(new Location(@params.TextDocument.Uri, codeLens.ActiveRange))
                select new CodeLens(codeLens.Range) { Data = data };
        }

        [JsonRpcMethod("codeLens/resolve")]
        public CodeLens ResolveCodeLens(JToken jsonParams)
        {
            var codeLens = Deserialize<CodeLens>(jsonParams);
            if (codeLens.Data is string data)
            {
                var location = Deserialize<Location>(data);
                IEnumerable<Services.Location>? references = _services.GetReferences(new PointerLocation(location.Uri, location.Range.Start));
                if (references is not null)
                {
                    int referenceCount = references.Count();
                    string text = referenceCount == 1 ? "1 reference" : $"{referenceCount} references";
                    codeLens.Command = new Command(text, "nevod.peekReferences",
                        new object[] { location.Uri, location.Range.Start });
                    return codeLens;
                }
                else
                    return codeLens;
            }
            else
                throw new Exception("Cannot resolve CodeLens");
        }

        [JsonRpcMethod("workspace/symbol")]
        public IEnumerable<Symbol> WorkspaceSymbol(JToken jsonParams)
        {
            var @params = Deserialize<WorkspaceSymbolParams>(jsonParams);
            return _services.GetFilteredSymbols(@params.Query);
        }

        [JsonRpcMethod("textDocument/prepareRename")]
        public PrepareRenameResponse? PrepareRename(JToken jsonParams)
        {
            var @params = Deserialize<PrepareRenameParams>(jsonParams);
            RenameInfo? renameRange = _services.GetRenameInfo(new PointerLocation(@params.TextDocument.Uri, @params.Position));
            if (renameRange is null)
                return null;
            return new PrepareRenameResponse(renameRange.Value.Range, renameRange.Value.Text);
        }

        [JsonRpcMethod("textDocument/rename")]
        public WorkspaceEdit? Rename(JToken jsonParams)
        {
            var @params = Deserialize<RenameParams>(jsonParams);
            IEnumerable<Services.TextEdit>? textEdits = _services.RenameSymbol(new PointerLocation(@params.TextDocument.Uri, @params.Position), @params.NewName);
            if (textEdits is null)
                return null;
            IEnumerable<IGrouping<Uri, TextEdit>> changes = from textEdit in textEdits
                group (TextEdit)textEdit by textEdit.Location.Uri;
            return new WorkspaceEdit { Changes = changes.ToDictionary(c => c.Key, c => c.ToArray()) };
        }

        [JsonRpcMethod("textDocument/completion")]
        public IEnumerable<CompletionItem>? Completion(JToken jsonParams)
        {
            var @params = Deserialize<CompletionParams>(jsonParams);
            var pointerLocation = new PointerLocation(@params.TextDocument.Uri, @params.Position);
            IEnumerable<Completion>? completions = _services.GetCompletions(pointerLocation);
            if (completions is null)
                return null;
            return from completion in completions
                let completionItemKind = completion.Kind switch
                {
                    CompletionKind.Field => CompletionItemKind.Field,
                    CompletionKind.Pattern => CompletionItemKind.Function,
                    CompletionKind.Namespace => CompletionItemKind.Module,
                    CompletionKind.Keyword => CompletionItemKind.Keyword,
                    CompletionKind.FilePath => CompletionItemKind.File,
                    CompletionKind.DirectoryPath => CompletionItemKind.Folder,
                    CompletionKind.TextAttribute => CompletionItemKind.Value,
                    CompletionKind.Token => CompletionItemKind.Value,
                    _ => throw new ArgumentOutOfRangeException(nameof(completion.Kind))
                }
                select new CompletionItem(completion.Text)
                {
                    LabelEx = new CompletionItem.CompletionItemLabelEx(completion.Text)
                    {
                        Detail = completion.Context is null ? null : $" (in {completion.Context})"
                    },
                    Kind = completionItemKind,
                    InsertText = completion.InsertText,
                    FilterText = completion.FilterText,
                    SortText = completion.SortText,
                    TextEdit = completion.TextEdit
                };
        }

        private static string Serialize<T>(T @object)
        {
            StringWriter stringWriter = new();
            s_jsonMessageFormatter.JsonSerializer.Serialize(stringWriter, @object);
            return stringWriter.ToString();
        }

        private static T Deserialize<T>(string json)
        {
            JsonTextReader reader = new(new StringReader(json));
            return s_jsonMessageFormatter.JsonSerializer.Deserialize<T>(reader) ?? throw CannotParseException(json);
        }

        private static T Deserialize<T>(JToken jsonParams) =>
            jsonParams.ToObject<T>(s_jsonMessageFormatter.JsonSerializer) ?? throw CannotParseException(jsonParams);

        private static Exception CannotParseException(object @object) => new Exception($"Cannot parse object: {@object}");

        private void EnsureNotStarted()
        {
            int wasStarted = Interlocked.Exchange(ref _isStarted, 1);
            if (wasStarted == 1)
                throw new Exception("Server already started");
        }

        private void Stop()
        {
            _jsonRpc.Dispose();
            _isStarted = 0;
        }

        private void EnsureStarted()
        {
            if (_isStarted == 0)
                throw new Exception("Server has not been started");
        }

        private void EnsureInitializedAndRunning()
        {
            EnsureStarted();
            if (!_isInitialized)
                throw new Exception("Server has not been initialized");
            if (_isShutDown)
                throw new Exception("Server has been shut down");
        }

        private IEnumerable<DocumentSymbol> ConvertSymbol(IEnumerable<Symbol> symbols) =>
            from symbol in symbols
            let children = symbol.Children is not null ? ConvertSymbol(symbol.Children).ToArray() : null
            select new DocumentSymbol(symbol.Name, SymbolKind.Field, symbol.Location.Range, symbol.NameLocation.Range)
            {
                Detail = symbol.Detail,
                Children = children
            };

        private async Task PublishDiagnosticsAsync(Uri uri, Services.Diagnostic[] diagnostics)
        {
            Diagnostic[] localDiagnostics = (from d in diagnostics
                select new Diagnostic(d.Range, d.Message)
                {
                    Source = "nevod",
                    Severity = DiagnosticSeverity.Error
                }).ToArray();
            PublishDiagnosticsParams publishDiagnosticsParams = new(uri, localDiagnostics);
            await _jsonRpc.NotifyWithParameterObjectAsync("textDocument/publishDiagnostics", publishDiagnosticsParams);
        }
    }

    internal class UriConverter : JsonConverter<Uri?>
    {
        public override void WriteJson(JsonWriter writer, Uri? uri, JsonSerializer serializer)
        {
            if (uri is not null)
            {
                string value = uri.ToString();
                if (value.Contains("://"))
                    value = $"{uri.Scheme}://{value.Substring(uri.Scheme.Length + 3).Replace(":", "%3A").Replace('\\', '/')}";
                writer.WriteValue(value);
            }
            else
                writer.WriteNull();
        }

        public override Uri? ReadJson(JsonReader reader, Type objectType, Uri? existingValue, bool hasExistingValue, JsonSerializer serializer) =>
            reader.TokenType switch
            {
                JsonToken.String when reader.Value is string value => new Uri(value.Replace("%3A", ":")),
                JsonToken.Null => null,
                _ => throw new InvalidOperationException($"Unsupported token type: {reader.TokenType}")
            };
    }
}
