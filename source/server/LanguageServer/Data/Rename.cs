namespace Nezaboodka.Nevod.LanguageServer
{
    public class RenameClientCapabilities
    {
        public bool? DynamicRegistration { get; set; }
        public bool? PrepareSupport { get; set; }
        public PrepareSupportDefaultBehavior? PrepareSupportDefaultBehavior { get; set; }
        public bool? HonorsChangeAnnotations { get; set; }
    }

    public enum PrepareSupportDefaultBehavior
    {
        Identifier = 1
    }

    public class RenameOptions
    {
        public bool? PrepareProvider { get; set; }
    }

    public class RenameParams : TextDocumentPositionParams
    {
        public string NewName { get; set; }

        public RenameParams(TextDocumentIdentifier textDocument, string newName) : base(textDocument) => NewName = newName;
    }

    public class PrepareRenameParams : TextDocumentPositionParams
    {
        public PrepareRenameParams(TextDocumentIdentifier textDocument) : base(textDocument) { }
    }

    public class PrepareRenameResponse
    {
        public Range Range { get; set; }
        public string Placeholder { get; set; }
        
        public PrepareRenameResponse(Range range, string placeholder)
        {
            Range = range;
            Placeholder = placeholder;
        }
    }
}
