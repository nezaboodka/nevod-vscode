namespace Nezaboodka.Nevod.LanguageServer
{
    public class DefinitionClientCapabilities
    {
        public bool? DynamicRegistration { get; set; }
        public bool LinkSupport { get; set; }

        public DefinitionClientCapabilities(bool linkSupport) => LinkSupport = linkSupport;
    }

    public class DefinitionParams : TextDocumentPositionParams
    {
        public DefinitionParams(TextDocumentIdentifier textDocument) : base(textDocument) { }
    }
}
