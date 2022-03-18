namespace Nezaboodka.Nevod.LanguageServer
{
    class CompletionClientCapabilities
    {
        public class ClientCompletionItem
        {
            public class CompetionItemTagSupport
            {
                public CompetionItemTagSupport[]? ValueSet { get; set; }
            }

            public class CompletionItemResolveSupport
            {
                public string[] Properties { get; set; }

                public CompletionItemResolveSupport(string[] properties) => Properties = properties;
            }

            public class CompletionItemInsertTextModeSupport
            {
                public InsertTextMode[] ValueSet { get; set; }

                public CompletionItemInsertTextModeSupport(InsertTextMode[] valueSet) => ValueSet = valueSet;
            }

            public bool? SnippetSupport { get; set; }
            public bool? CommitCharactersSupport { get; set; }
            public MarkupKind[]? DocumentationFormat { get; set; }
            public bool? DeprecatedSupport { get; set; }
            public bool? PreselectSupport { get; set; }
            public CompetionItemTagSupport? TagSupport { get; set; }
            public bool? InsertReplaceSupport { get; set; }
            public CompletionItemResolveSupport? ResolveSupport { get; set; }
            public CompletionItemInsertTextModeSupport? InsertTextModeSupport { get; set; }
            public bool? LabelDetailsSupport { get; set; }
        }

        public class ClientCompletionItemKind
        {
            ClientCompletionItemKind[]? ValueSet { get; set; }
        }

        public bool? DynamicRegistration { get; set; }
        public ClientCompletionItem? CompletionItem { get; set; }
        public ClientCompletionItemKind? CompletionItemKind { get; set; }
        public bool? ContextSupport { get; set; }
        public InsertTextMode? InsertTextMode { get; set; }
    }

    public class CompletionOptions : WorkDoneProgressOptions
    {
        public class CompletionItemOption
        {
            public bool? LabelDetailsSupport { get; set; }
        }

        public string[]? TriggerCharacters { get; set; }
        public string[]? AllCommitCharacters { get; set; }
        public bool? ResolveProvider { get; set; }
        public CompletionItemOption? CompletionItem { get; set; }
    }
    
    public class CompletionParams : TextDocumentPositionParams
    {
        public CompletionContext? Context { get; set; }
        
        public CompletionParams(TextDocumentIdentifier textDocument) : base(textDocument)
        {
        }
    }
    
    public enum CompletionTriggerKind {
        Invoked = 1,
        TriggerCharacter,
        TriggerForIncompleteCompletions
    }
    
    public class CompletionContext 
    {
        public CompletionTriggerKind TriggerKind { get; set; }
        public string? TriggerCharacter { get; set; }
        
        public CompletionContext(CompletionTriggerKind triggerKind)
        {
            TriggerKind = triggerKind;
        }
    }
    
    public class CompletionList 
    {
        public bool IsIncomplete { get; set; }
        private CompletionItem[] Items { get; set; }

        public CompletionList(bool isIncomplete, CompletionItem[] items)
        {
            IsIncomplete = isIncomplete;
            Items = items;
        }
    }

    public class CompletionItem
    {
        public class CompletionItemLabelEx
        {
            public string Label { get; set; }
            public string? Detail { get; set; }
            public string? Description { get; set; }
            
            public CompletionItemLabelEx(string label)
            {
                Label = label;
            }
        }

        public string Label { get; set; }
        // Custom field, handled by client
        public CompletionItemLabelEx? LabelEx { get; set; }
        public CompletionItemLabelDetails? LabelDetails { get; set; }
        public CompletionItemKind? Kind { get; set; }
        public CompletionItemTag[]? Tags { get; set; }
        public string? Detail { get; set; }
        public string? Documentation { get; set; }
        public bool? Deprecated { get; set; }
        public bool? Preselect { get; set; }
        public string? SortText { get; set; }
        public string? FilterText { get; set; }
        public string? InsertText { get; set; }
        public InsertTextFormat? InsertTextFormat { get; set; }
        public InsertTextMode? InsertTextMode { get; set; }
        public TextEdit? TextEdit { get; set; }
        public TextEdit[]? AdditionalTextEdits { get; set; }
        public string[]? CommitCharacters { get; set; }
        public Command? Command { get; set; }
        public object? Data { get; set; }

        public CompletionItem(string label)
        {
            Label = label;
        }
    }
    
    public class CompletionItemLabelDetails
    {
        public string? Detail { get; set; }
        public string? Description { get; set; }
    }
    
    public enum InsertTextFormat {
        PlainText = 1,
        Snippet
    }

    public enum CompletionItemTag
    {
        Deprecated = 1
    }

    public enum InsertTextMode
    {
        AsIs = 1,
        AdjustIndentation = 2
    }

    public enum CompletionItemKind
    {
        Text = 1,
        Method,
        Function,
        Constructor,
        Field,
        Variable,
        Class,
        Interface,
        Module,
        Property,
        Unit,
        Value,
        Enum,
        Keyword,
        Snippet,
        Color,
        File,
        Reference,
        Folder,
        EnumMember,
        Constant,
        Struct,
        Event,
        Operator,
        TypeParameter
    }
}
