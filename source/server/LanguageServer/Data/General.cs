using System;

namespace Nezaboodka.Nevod.LanguageServer
{
    public class InitializeParams : WorkDoneProgressParams
    {
        public class ClientInfoParams
        {
            public string Name { get; set; }
            public string? Version { get; set; } = null;

            public ClientInfoParams(string name) => Name = name;
        }

        public int? ProcessId { get; set; }
        public ClientInfoParams? ClientInfo { get; set; }
        public string? Locale { get; set; }
        public string? RootPath { get; set; }
        public Uri? RootUri { get; set; }
        public ClientCapabilities Capabilities { get; set; }
        public object? InitializationOptions { get; set; }
        public TraceValue? Trace { get; set; }
        public WorkspaceFolder[]? WorkspaceFolders { get; set; }

        public InitializeParams(ClientCapabilities capabilities) => Capabilities = capabilities;
    }

    public class TextDocumentClientCapabilities
    {
        TextDocumentSyncClientCapabilities? Synchronization { get; set; }
        CompletionClientCapabilities? completion { get; set; }
        HoverClientCapabilities? Hover { get; set; }
        SignatureHelpClientCapabilities? SignatureHelp { get; set; }
        DeclarationClientCapabilities? Declaration { get; set; }
        DefinitionClientCapabilities? Definition { get; set; }
        TypeDefinitionClientCapabilities? TypeDefinition { get; set; }
        ImplementationClientCapabilities? Implementation { get; set; }
        ReferenceClientCapabilities? References { get; set; }
        DocumentHighlightClientCapabilities? DocumentHighlight { get; set; }
        DocumentSymbolClientCapabilities? DocumentSymbol { get; set; }
        CodeActionClientCapabilities? CodeAction { get; set; }
        CodeLensClientCapabilities? CodeLens { get; set; }
        DocumentLinkClientCapabilities? DocumentLink { get; set; }
        DocumentColorClientCapabilities? ColorProvider { get; set; }
        DocumentFormattingClientCapabilities? Formatting { get; set; }
        DocumentRangeFormattingClientCapabilities? RangeFormatting { get; set; }
        DocumentOnTypeFormattingClientCapabilities? OnTypeFormatting { get; set; }
        RenameClientCapabilities? Rename { get; set; }
        PublishDiagnosticsClientCapabilities? PublishDiagnostics { get; set; }
        FoldingRangeClientCapabilities? FoldingRange { get; set; }
        SelectionRangeClientCapabilities? SelectionRange { get; set; }
        LinkedEditingRangeClientCapabilities? LinkedEditingRange { get; set; }
        CallHierarchyClientCapabilities? CallHierarchy { get; set; }
        SemanticTokensClientCapabilities? SemanticTokens { get; set; }
        MonikerClientCapabilities? Moniker { get; set; }
    }

    public class ClientCapabilities
    {
        public class WorkspaceClientCapabilities
        {
            public class FileOperationsClientCapabilities
            {
                public bool? DynamicRegistration { get; set; }
                public bool? DidCreate { get; set; }
                public bool? WillCreate { get; set; }
                public bool? DidRename { get; set; }
                public bool? WillRename { get; set; }
                public bool? DidDelete { get; set; }
                public bool? WillDelete { get; set; }
            }

            public bool? ApplyEdit { get; set; }
            public WorkspaceEditClientCapabilities? WorkspaceEdit { get; set; }
            public DidChangeConfigurationClientCapabilities? DidChangeConfiguration { get; set; }
            public DidChangeWatchedFilesClientCapabilities? DidChangeWatchedFiles { get; set; }
            public WorkspaceSymbolClientCapabilities? Symbol { get; set; }
            public ExecuteCommandClientCapabilities? ExecuteCommand { get; set; }
            public bool? WorkspaceFolders { get; set; }
            public bool? Configuration { get; set; }
            public SemanticTokensWorkspaceClientCapabilities? SemanticTokens { get; set; }
            public CodeLensWorkspaceClientCapabilities? CodeLens { get; set; }
            public FileOperationsClientCapabilities? FileOperations { get; set; }
        }

        public class WindowClientCapabilities
        {
            public bool? WorkDoneProgress { get; set; }
            public ShowMessageRequestClientCapabilities? ShowMessage { get; set; }
            public ShowDocumentClientCapabilities? ShowDocument { get; set; }
        }

        public class GeneralClientCapabilities
        {
            public class StaleRequestSupportClientCapabilities
            {
                public bool Cancel { get; set; }
                public string[] RetryOnContentModified { get; set; }

                public StaleRequestSupportClientCapabilities(bool cancel, string[] retryOnContentModified)
                {
                    Cancel = cancel;
                    RetryOnContentModified = retryOnContentModified;
                }
            }

            public StaleRequestSupportClientCapabilities? StaleRequestSupport { get; set; }
            public RegularExpressionsClientCapabilities? RegularExpressions { get; set; }
            public MarkdownClientCapabilities? Markdown { get; set; }
        }

        public WorkspaceClientCapabilities? Workspace { get; set; }
        public TextDocumentClientCapabilities? TextDocument { get; set; }
        public WindowClientCapabilities? Window { get; set; }
        public GeneralClientCapabilities? General { get; set; }
        public object? Experimental { get; set; }
    }

    public class InitializeResult
    {
        public class InitializeResultServerInfo
        {
            public string Name { get; set; }
            public string? Version { get; set; }

            public InitializeResultServerInfo(string name) => Name = name;
        }

        public ServerCapabilities Capabilities { get; set; }
        public InitializeResultServerInfo? ServerInfo { get; set; }

        public InitializeResult(ServerCapabilities capabilities) => Capabilities = capabilities;
    }

    public class InitializeError
    {
        public bool Retry { get; set; }

        public InitializeError(bool retry) => Retry = retry;
    }

    public class ServerCapabilities
    {
        public class WorkspaceServerCapabilities
        {
            public class FileOperationsServerCapabilities
            {
                public FileOperationRegistrationOptions? DidCreate { get; set; }
                public FileOperationRegistrationOptions? WillCreate { get; set; }
                public FileOperationRegistrationOptions? DidRename { get; set; }
                public FileOperationRegistrationOptions? WillRename { get; set; }
                public FileOperationRegistrationOptions? DidDelete { get; set; }
                public FileOperationRegistrationOptions? WillDelete { get; set; }
            }

            public WorkspaceFoldersServerCapabilities? WorkspaceFolders { get; set; }
            public FileOperationsServerCapabilities? FileOperations { get; set; }
        }

        public TextDocumentSyncOptions? TextDocumentSync { get; set; }
        public CompletionOptions? CompletionProvider { get; set; }
        public bool? HoverProvider { get; set; }
        public SignatureHelpOptions? SignatureHelpProvider { get; set; }
        public bool? DeclarationProvider { get; set; }
        public bool? DefinitionProvider { get; set; }
        public bool? TypeDefinitionProvider { get; set; }
        public bool? ImplementationProvider { get; set; }
        public bool? ReferencesProvider { get; set; }
        public bool? DocumentHighlightProvider { get; set; }
        public bool? DocumentSymbolProvider { get; set; }
        public CodeActionOptions? CodeActionProvider { get; set; }
        public CodeLensOptions? CodeLensProvider { get; set; }
        public DocumentLinkOptions? DocumentLinkProvider { get; set; }
        public bool? ColorProvider { get; set; }
        public bool? DocumentFormattingProvider { get; set; }
        public bool? DocumentRangeFormattingProvider { get; set; }
        public DocumentOnTypeFormattingOptions? DocumentOnTypeFormattingProvider { get; set; }
        public RenameOptions? RenameProvider { get; set; }
        public bool? FoldingRange { get; set; }
        public ExecuteCommandOptions? ExecuteCommandProvider { get; set; }
        public bool? SelectionRangeProvider { get; set; }
        public bool? LinkedEditingRangeProvider { get; set; }
        public bool? CallHierarchyProvider { get; set; }
        public SemanticTokensOptions? SemanticTokensProvider { get; set; }
        public bool? MonikerProvider { get; set; }
        public bool? WorkspaceSymbolProvider { get; set; }
        public WorkspaceServerCapabilities? Workspace { get; set; }
        public object? Experimental { get; set; }
    }

    public class InitializedParams { }
}
