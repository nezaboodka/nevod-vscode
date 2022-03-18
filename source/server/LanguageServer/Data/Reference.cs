namespace Nezaboodka.Nevod.LanguageServer
{
    public class ReferenceClientCapabilities
    {
        public bool? DynamicRegistration { get; set; }
    }

    public class ReferenceContext
    {
        public bool IncludeDeclaration { get; set; }

        public ReferenceContext(bool includeDeclaration) => IncludeDeclaration = includeDeclaration;
    }

    public class ReferenceParams : TextDocumentPositionParams
    {
        public ReferenceContext Context { get; set; }

        public ReferenceParams(TextDocumentIdentifier textDocument, ReferenceContext context) : base(textDocument) => Context = context;
    }
}
