namespace Nezaboodka.Nevod.LanguageServer
{
    public enum TokenFormat
    {
        Relative
    }

    public class SemanticTokensLegend
    {
        public string[] TokenTypes { get; set; }
        public string[] TokenModifiers { get; set; }

        public SemanticTokensLegend(string[] tokenTypes, string[] tokenModifiers)
        {
            TokenTypes = tokenTypes;
            TokenModifiers = tokenModifiers;
        }
    }

    public class SemanticTokensClientCapabilities
    {
        public class SemanticTokensClientRequests
        {
            public bool? Range { get; set; }
            public object? Full { get; set; }
        }

        public bool? DynamicRegistration { get; set; }
        public SemanticTokensClientRequests Requests { get; set; }
        public string[] TokenTypes { get; set; }
        public string[] TokenModifiers { get; set; }
        public TokenFormat[] Formats { get; set; }
        public bool? OverlappingTokenSupport { get; set; }
        public bool? MultilineTokenSupport { get; set; }

        public SemanticTokensClientCapabilities(SemanticTokensClientRequests requests, string[] tokenTypes,
            string[] tokenModifiers, TokenFormat[] formats)
        {
            Requests = requests;
            TokenTypes = tokenTypes;
            TokenModifiers = tokenModifiers;
            Formats = formats;
        }
    }

    public class SemanticTokensOptions : WorkDoneProgressOptions
    {
        public SemanticTokensLegend Legend { get; set; }
        public bool? Range { get; set; }
        public object? Full { get; set; }

        public SemanticTokensOptions(SemanticTokensLegend legend) => Legend = legend;
    }

    public class SemanticTokensWorkspaceClientCapabilities
    {
        public bool? RefreshSupport { get; set; }
    }
}
