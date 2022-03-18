namespace Nezaboodka.Nevod.LanguageServer
{
    public enum TextDocumentSyncKind
    {
        None,
        Full,
        Incremental
    }

    public class TextDocumentSyncOptions
    {
        public bool? OpenClose { get; set; }
        public TextDocumentSyncKind? Change { get; set; }
        public bool? WillSave { get; set; }
        public bool? WillSaveWaitUntil { get; set; }
        public bool? Save { get; set; }
    }

    public class DidOpenTextDocumentParams
    {
        public TextDocumentItem TextDocument { get; set; }

        public DidOpenTextDocumentParams(TextDocumentItem textDocument) => TextDocument = textDocument;
    }

    public class DidChangeTextDocumentParams
    {
        public VersionedTextDocumentIdentifier TextDocument { get; set; }
        public TextDocumentContentChangeEvent[] ContentChanges { get; set; }

        public DidChangeTextDocumentParams(VersionedTextDocumentIdentifier textDocument, TextDocumentContentChangeEvent[] contentChanges)
        {
            TextDocument = textDocument;
            ContentChanges = contentChanges;
        }
    }

    public class TextDocumentContentChangeEvent
    {
        public Range? Range { get; set; }
        public int? RangeLength { get; set; }
        public string Text { get; set; }

        public TextDocumentContentChangeEvent(string text) => Text = text;
    }

    public class DidCloseTextDocumentParams
    {
        public TextDocumentIdentifier TextDocument { get; set; }

        public DidCloseTextDocumentParams(TextDocumentIdentifier textDocument) => TextDocument = textDocument;
    }

    public class TextDocumentSyncClientCapabilities
    {
        public bool? DynamicRegistration { get; set; }
        public bool? WillSave { get; set; }
        public bool? WillSaveWaitUntil { get; set; }
        public bool? DidSave { get; set; }
    }
}
