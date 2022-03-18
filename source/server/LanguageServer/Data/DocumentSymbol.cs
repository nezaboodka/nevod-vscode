namespace Nezaboodka.Nevod.LanguageServer
{
    public class DocumentSymbolClientCapabilities
    {
        public class ClientSymbolKind
        {
            public SymbolKind[]? ValueSet { get; set; }
        }

        public class ClientTagSupport
        {
            public SymbolTag[] ValueSet { get; set; }

            public ClientTagSupport(SymbolTag[] valueSet) => ValueSet = valueSet;
        }

        public bool? DynamicRegistration { get; set; }
        public ClientSymbolKind? SymbolKind { get; set; }
        public bool? HierarchicalDocumentSymbolSupport { get; set; }
        public ClientTagSupport? TagSupport { get; set; }
        public bool? LabelSupport { get; set; }
    }

    public class DocumentSymbolParams : WorkDoneProgressParams
    {
        public TextDocumentIdentifier TextDocument { get; set; }

        public DocumentSymbolParams(TextDocumentIdentifier textDocument) => TextDocument = textDocument;
    }

    public enum SymbolKind
    {
        File = 1,
        Module,
        Namespace,
        Package,
        Class,
        Method,
        Property,
        Field,
        Constructor,
        Enum,
        Interface,
        Function,
        Variable,
        Constant,
        String,
        Number,
        Boolean,
        Array,
        Object,
        Key,
        Null,
        EnumMember,
        Struct,
        Event,
        Operator,
        TypeParameter,
    }

    public enum SymbolTag
    {
        Deprecated = 1
    }

    public class DocumentSymbol
    {
        public string Name { get; set; }
        public string? Detail { get; set; }
        public SymbolKind Kind { get; set; }
        public SymbolTag[]? Tags { get; set; }
        public bool? Deprecated { get; set; }
        public Range Range { get; set; }
        public Range SelectionRange { get; set; }
        public DocumentSymbol[]? Children { get; set; }

        public DocumentSymbol(string name, SymbolKind kind, Range range, Range selectionRange)
        {
            Name = name;
            Kind = kind;
            Range = range;
            SelectionRange = selectionRange;
        }
    }
}
