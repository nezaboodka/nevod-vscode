namespace Nezaboodka.Nevod.LanguageServer
{
    public class DocumentFormattingClientCapabilities
    {
        public bool? DynamicRegistration { get; set; }
    }

    public class DocumentFormattingParams : WorkDoneProgressParams
    {
        public TextDocumentIdentifier TextDocument { get; set; }
        public FormattingOptions Options { get; set; }

        public DocumentFormattingParams(TextDocumentIdentifier textDocument, FormattingOptions options)
        {
            TextDocument = textDocument;
            Options = options;
        }
    }

    public class DocumentRangeFormattingParams : WorkDoneProgressParams
    {
        public TextDocumentIdentifier TextDocument { get; set; }
        public Range Range { get; set; }
        public FormattingOptions Options { get; set; }

        public DocumentRangeFormattingParams(TextDocumentIdentifier textDocument, Range range, FormattingOptions options)
        {
            TextDocument = textDocument;
            Range = range;
            Options = options;
        }
    }

    public class FormattingOptions 
    {
        public uint TabSize { get; set; }
        public bool InsertSpaces { get; set; }
        // Custom field, handled by client
        public string? NewLine { get; set; }
        public bool? TrimTrailingWhitespace { get; set; }
        public bool? InsertFinalNewline { get; set; }
        public bool? TrimFinalNewlines { get; set; }

        public static implicit operator Services.FormattingOptions(FormattingOptions options) =>
            new(options.TabSize, options.InsertSpaces, options.NewLine);
    }
}
