namespace Nezaboodka.Nevod.LanguageServer
{
    public class DocumentLinkClientCapabilities
    {
        public bool? DynamicRegistration { get; set; }
        public bool? TooltipSupport { get; set; }
    }

    public class DocumentLinkOptions : WorkDoneProgressOptions
    {
        public bool? ResolveProvider { get; set; }
    }
}
