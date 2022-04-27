using System;
using System.Collections.Generic;

namespace Nezaboodka.Nevod.Services
{
    public delegate void DiagnosticsPublisher(Uri uri, Diagnostic[] diagnostics);

    public interface IServices
    {
        public event DiagnosticsPublisher? PublishDiagnostics;

        void OpenDocument(Uri uri, string text);

        void UpdateDocument(Uri uri, string text);

        void CloseDocument(Uri uri);

        void DeleteDocument(Uri uri);

        void UpdateConfiguration(Configuration configuration);

        Location? GetDefinition(PointerLocation location);

        IEnumerable<Location>? GetReferences(PointerLocation location);

        IEnumerable<Symbol> GetDocumentSymbols(Uri uri);

        IEnumerable<Symbol> GetFilteredSymbols(string query);

        IEnumerable<CodeLens> GetCodeLens(Uri uri);

        RenameInfo? GetRenameInfo(PointerLocation location);

        IEnumerable<TextEdit>? RenameSymbol(PointerLocation location, string newName);

        IEnumerable<Completion>? GetCompletions(PointerLocation location);

        IEnumerable<TextEdit>? FormatDocument(Uri uri, FormattingOptions options);

        IEnumerable<TextEdit>? FormatDocumentRange(Uri uri, Range range, FormattingOptions options);
    }
}
