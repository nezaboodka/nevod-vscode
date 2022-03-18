namespace Nezaboodka.Nevod.LanguageServer
{
    public class SignatureHelpClientCapabilities
    {
        public class ClientSignatureInformation
        {
            public class SignatureParentInformation
            {
                public bool? LabelOffsetSupport { get; set; }
            }

            public MarkupKind[]? DocumentationFormat { get; set; }
            public SignatureParentInformation? ParentInformation { get; set; }
            public bool? ActiveParameterSupport { get; set; }

        }

        public bool? DynamicRegistration { get; set; }
        public ClientSignatureInformation? SignatureInformation { get; set; }
        public bool? ContextSupport { get; set; }
    }

    public class SignatureHelpOptions : WorkDoneProgressOptions
    {
        public string[]? TriggerCharacters { get; set; }
        public string[]? RetriggerCharacters { get; set; }
    }
}
