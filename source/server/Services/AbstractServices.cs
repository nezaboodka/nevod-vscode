using System;
using System.Collections.Generic;
using System.Linq;

namespace Nezaboodka.Nevod.Services
{
    public abstract class AbstractServices : IServices
    {
        private DiagnosticsPublisher? _publishDiagnostics;
        private readonly Dictionary<Syntax, Location[]> _references = new();
        private readonly Dictionary<Uri, Symbol[]> _symbols = new();
        private readonly Dictionary<Uri, CodeLens[]> _codeLens = new();
        private SyntaxInfoTable _syntaxInfoTable;
        private readonly PatternReferenceFinder _patternReferenceFinder = new();
        private readonly FieldReferenceFinder _fieldReferenceFinder = new();
        private readonly SymbolFinder _symbolFinder = new();
        private readonly PatternFinder _patternFinder = new();
        private readonly SyntaxInfoTableBuilder _syntaxInfoTableBuilder = new();
        private readonly RenameProvider _renameProvider = new();
        private readonly CompletionsProvider _completionsProvider = new();

        private protected Dictionary<Uri, Document> Documents { get; private set; } = new();

        internal AbstractServices()
        {
            _syntaxInfoTable = _syntaxInfoTableBuilder.BuildSyntaxInfoTable(Documents, out Dictionary<Uri, Document> newDocuments);
            Documents = newDocuments;
        }

        public event DiagnosticsPublisher? PublishDiagnostics
        {
            add
            {
                _publishDiagnostics += value;
                CollectAndPublishDiagnostics();
            }
            remove => _publishDiagnostics -= value;
        }

        public virtual void OpenDocument(Uri uri, string text)
        {
            if (!Documents.ContainsKey(uri))
                Documents.Add(uri, new Document(uri, text, true));
            else
            {
                Document document = Documents[uri];
                document.Update(text);
                document.IsTrackedByServer = true;
            }
        }

        public virtual void UpdateDocument(Uri uri, string text)
        {
            Document document = Documents[uri];
            document.Update(text);
            RebuildSyntaxInfo();
        }

        public virtual void CloseDocument(Uri uri) => Documents.Remove(uri);

        public void DeleteDocument(Uri uri)
        {
            if (Documents.ContainsKey(uri))
            {
                Documents.Remove(uri);
                RebuildSyntaxInfo();
            }
        }

        public Location? GetDefinition(PointerLocation location)
        {
            Lexeme? lexeme = Utils.GetPointedLexeme(location, out Syntax? parent, _syntaxInfoTable, Documents);
            Location? definition = null;
            if (lexeme is not null)
            {
                Document document = Documents[location.Uri];
                Location lexemeLocation = Utils.GetLexemeLocation(lexeme, document);
                if (lexemeLocation.Contains(location))
                    switch (parent)
                    {
                        case PatternReferenceSyntax reference:
                            if (lexeme.TokenId is TokenId.Identifier &&
                                reference.ReferencedPattern != null)
                                definition = Utils.GetPatternNameLocation(reference.ReferencedPattern, _syntaxInfoTable, Documents);
                            break;
                        case ExtractionSyntax extraction:
                            if (lexeme.TokenId is TokenId.Identifier)
                            {
                                ExtractionSyntaxInfo extractionInfo = _syntaxInfoTable.GetSyntaxInfo(extraction);
                                ISyntaxInfo<FieldSyntax>? extractionFieldInfo = GetFieldDefinition(extractionInfo.Pattern, extraction.FieldName);
                                if (extractionFieldInfo is not null)
                                    definition = Utils.TrimLocation(extractionFieldInfo.Location, Documents);
                            }
                            break;
                        case FieldReferenceSyntax fieldReference:
                            if (lexeme.TokenId is TokenId.Identifier)
                            {
                                FieldReferenceSyntaxInfo fieldReferenceInfo = _syntaxInfoTable.GetSyntaxInfo(fieldReference);
                                ISyntaxInfo<FieldSyntax>? extractionFieldInfo = GetFieldDefinition(fieldReferenceInfo.Pattern, fieldReference.FieldName);
                                if (extractionFieldInfo is not null)
                                    definition = Utils.TrimLocation(extractionFieldInfo.Location, Documents);
                            }
                            break;
                        case ExtractionFromFieldSyntax extractionFromField:
                            if (lexeme.TokenId is TokenId.Identifier)
                            {
                                ExtractionFromFieldSyntaxInfo extractionFromFieldInfo = _syntaxInfoTable.GetSyntaxInfo(extractionFromField);
                                // Pointed lexeme is field name
                                if (ReferenceEquals(extractionFromField.Children[0], lexeme))
                                {
                                    ISyntaxInfo<FieldSyntax>? fieldInfo = GetFieldDefinition(
                                        extractionFromFieldInfo.Pattern,
                                        extractionFromField.FieldName);
                                    if (fieldInfo is not null)
                                        definition = Utils.TrimLocation(fieldInfo.Location, Documents);
                                }
                                // Pointed lexeme is from field name
                                else if (extractionFromField.FromFieldName is not null &&
                                         extractionFromFieldInfo.PatternReference.ReferencedPattern is not null)
                                {
                                    ISyntaxInfo<FieldSyntax>? fieldInfo = GetFieldDefinition(
                                        extractionFromFieldInfo.PatternReference.ReferencedPattern,
                                        extractionFromField.FromFieldName);
                                    if (fieldInfo is not null)
                                        definition = Utils.TrimLocation(fieldInfo.Location, Documents);
                                }
                            }
                            break;
                        case RequiredPackageSyntax requiredPackage:
                            if (lexeme.TokenId is TokenId.StringLiteral && 
                                Uri.TryCreate(requiredPackage.FullPath, UriKind.Absolute, out Uri? uri))
                                definition = new PointerLocation(uri, new Position(0, 0));
                            break;
                    }
            }
            return definition;
        }

        public IEnumerable<Location>? GetReferences(PointerLocation location)
        {
            Lexeme? lexeme = Utils.GetPointedLexeme(location, out Syntax? parent, _syntaxInfoTable, Documents);
            Location[]? references = null;
            if (lexeme is not null)
            {
                LinkedPackageSyntax[] packages = Utils.GetAllLinkedPackages(_syntaxInfoTable, Documents);
                Document document = Documents[location.Uri];
                Location lexemeLocation = Utils.GetLexemeLocation(lexeme, document);
                if (lexemeLocation.Contains(location))
                    switch (parent)
                    {
                        case PatternSyntax pattern:
                            if (lexeme.TokenId is TokenId.Identifier &&
                                pattern.Name is not null &&
                                !_references.TryGetValue(lexeme, out references))
                            {
                                references = _patternReferenceFinder.FindPatternReferences(pattern, _syntaxInfoTable, Documents, packages);
                                _references.Add(lexeme, references);
                            }
                            break;
                        case FieldSyntax field:
                            if (lexeme.TokenId is TokenId.Identifier &&
                                !_references.TryGetValue(lexeme, out references))
                            {
                                references = _fieldReferenceFinder.FindFieldReferences(field, _syntaxInfoTable, Documents, packages);
                                _references.Add(lexeme, references);
                            }
                            break;
                    }
            }
            return references is not null ? Array.AsReadOnly(references) : null;
        }

        public IEnumerable<Symbol> GetDocumentSymbols(Uri uri)
        {
            if (!_symbols.TryGetValue(uri, out Symbol[]? symbols))
            {
                SyntaxInfo<LinkedPackageSyntax> linkedPackageSyntaxInfo = _syntaxInfoTable.GetLinkedPackageSyntaxInfo(uri);
                symbols = _symbolFinder.FindSymbols(_syntaxInfoTable, Documents, linkedPackageSyntaxInfo.Syntax).ToArray();
                _symbols[uri] = symbols;
            }
            return Array.AsReadOnly(symbols);
        }

        public IEnumerable<Symbol> GetFilteredSymbols(string query)
        {
            IEnumerable<Symbol> result = Enumerable.Empty<Symbol>();
            result = Documents.Keys.Aggregate(result, (current, uri) => current.Concat(GetDocumentSymbols(uri)));
            return result.Where(s => s.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<CodeLens> GetCodeLens(Uri uri)
        {
            if (!_codeLens.TryGetValue(uri, out CodeLens[]? codeLens))
            {
                SyntaxInfo<LinkedPackageSyntax> linkedPackageSyntaxInfo = _syntaxInfoTable.GetLinkedPackageSyntaxInfo(uri);
                PatternSyntax[] patterns = _patternFinder.FindPatterns(linkedPackageSyntaxInfo.Syntax);
                codeLens = (from pattern in patterns
                            where pattern.Name is not null
                            let info = _syntaxInfoTable.GetSyntaxInfo(pattern)
                            let range = Utils.TrimLocation(info.Location, Documents).Range
                            let activeRange = Utils.GetPatternNameLocation(info.Syntax, _syntaxInfoTable, Documents).Range
                            select new CodeLens(range, activeRange)).ToArray();
                _codeLens.Add(uri, codeLens);
            }
            return Array.AsReadOnly(codeLens);
        }

        public RenameInfo? GetRenameInfo(PointerLocation location) => 
            _renameProvider.GetRenameInfo(location, _syntaxInfoTable, Documents);

        public IEnumerable<TextEdit>? RenameSymbol(PointerLocation location, string newName) =>
            _renameProvider.RenameSymbol(location, newName, _syntaxInfoTable, Documents);

        public IEnumerable<Completion>? GetCompletions(PointerLocation location) =>
            _completionsProvider.GetCompletions(location, _syntaxInfoTable, Documents);

        private protected void RebuildSyntaxInfo()
        {
            _symbols.Clear();
            _references.Clear();
            _codeLens.Clear();
            var serverTrackedDocuments = Documents
                .Where(p => p.Value.IsTrackedByServer)
                .ToDictionary(p => p.Key, p => p.Value);
            _syntaxInfoTable = _syntaxInfoTableBuilder.BuildSyntaxInfoTable(serverTrackedDocuments, out Dictionary<Uri, Document> newDocuments);
            Documents = newDocuments;
            CollectAndPublishDiagnostics();
        }

        private void CollectAndPublishDiagnostics()
        {
            if (_publishDiagnostics != null)
            {
                IEnumerable<ISyntaxInfo<LinkedPackageSyntax>> packages = from d in Documents.Values
                                                                         select _syntaxInfoTable.GetLinkedPackageSyntaxInfo(d.Uri);
                foreach (ISyntaxInfo<LinkedPackageSyntax> info in packages)
                {
                    Uri uri = info.Location.Uri;
                    Diagnostic[] diagnostics = info.Syntax.Errors.Select(error => CreateDiagnostic(uri, error)).ToArray();
                    _publishDiagnostics(uri, diagnostics);
                }
            }
        }

        private ISyntaxInfo<FieldSyntax>? GetFieldDefinition(PatternSyntax pattern, string fieldName)
        {
            FieldSyntax? field = pattern.Fields.FirstOrDefault(f => f.Name == fieldName);
            return field is not null ? _syntaxInfoTable.GetSyntaxInfo(field) : null;
        }

        private Diagnostic CreateDiagnostic(Uri uri, Error error)
        {
            Document document = Documents[uri];
            return new Diagnostic(
                new Range(document.PositionAt(error.ErrorRange.Start), document.PositionAt(error.ErrorRange.End)),
                error.ErrorMessage
            );
        }
    }
}
