namespace Nezaboodka.Nevod.LanguageServer
{
    public class FoldingRangeClientCapabilities
    {
        public bool? DynamicRegistration { get; set; }
        public uint? RangeLimit { get; set; }
        public bool? LineFoldingOnly { get; set; }
    }
}
