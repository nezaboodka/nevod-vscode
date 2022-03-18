namespace Nezaboodka.Nevod.LanguageServer
{
    public class CodeLensClientCapabilities
    {
        public bool? DynamicRegistration { get; set; }
    }

    public class CodeLensOptions : WorkDoneProgressOptions
    {
        public bool? ResolveProvider { get; set; }
    }

    public class CodeLensParams : WorkDoneProgressParams
    {
        public TextDocumentIdentifier TextDocument { get; }

        public CodeLensParams(TextDocumentIdentifier textDocument) => TextDocument = textDocument;
    }

    public class CodeLens
    {
        public Range Range { get; set; }
        public Command? Command { get; set; }
        public object? Data { get; set; }

        public CodeLens(Range range) => Range = range;
    }

    public class CodeLensWorkspaceClientCapabilities
    {
        public bool? RefreshSupport { get; set; }
    }
}
