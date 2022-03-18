using Newtonsoft.Json;

namespace Nezaboodka.Nevod.LanguageServer
{
    public class CodeActionClientCapabilities
    {
        public class ClientCodeActionLiteralSupport
        {
            public class ClientCodeActionKind
            {
                private CodeActionKind[] ValueSet { get; set; }

                public ClientCodeActionKind(CodeActionKind[] valueSet) => ValueSet = valueSet;
            }

            public ClientCodeActionKind CodeActionKind { get; set; }

            public ClientCodeActionLiteralSupport(ClientCodeActionKind codeActionKind) =>
                CodeActionKind = codeActionKind;
        }

        public class ClientResolveSupport
        {
            public string[] Properties { get; set; }

            public ClientResolveSupport(string[] properties) => Properties = properties;
        }

        public bool? DynamicRegistration { get; set; }
        public ClientCodeActionLiteralSupport? CodeActionLiteralSupport { get; set; }
        public bool? IsPreferredSupport { get; set; }
        public bool? DisabledSupport { get; set; }
        public bool? DataSupport { get; set; }
        public ClientResolveSupport? ResolveSupport { get; set; }
        public bool? HonorsChangeAnnotations { get; set; }
    }

    public class CodeActionOptions : WorkDoneProgressOptions
    {
        public CodeActionKind[]? CodeActionKinds { get; set; }
        public bool? ResolveProvider { get; set; }
    }

    public enum CodeActionKind
    {
        [JsonProperty("")] Empty,
        [JsonProperty("quickfix")] QuickFix,
        [JsonProperty("refactor")] Refactor,
        [JsonProperty("refactor.extract")] RefactorExtract,
        [JsonProperty("refactor.inline")] RefactorInline,
        [JsonProperty("refactor.rewrite")] RefactorRewrite,
        [JsonProperty("source")] Source,
        [JsonProperty("source.organizeImports")] SourceOrganizeImports,
    }
}
