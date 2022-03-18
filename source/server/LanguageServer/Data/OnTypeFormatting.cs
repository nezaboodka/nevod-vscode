namespace Nezaboodka.Nevod.LanguageServer
{
    public class DocumentOnTypeFormattingClientCapabilities
    {
        public bool? DynamicRegistration { get; set; }
    }

    public class DocumentOnTypeFormattingOptions
    {
        public string FirstTriggerCharacter { get; set; }
        public string[]? MoreTriggerCharacter { get; set; }

        public DocumentOnTypeFormattingOptions(string firstTriggerCharacter) =>
            FirstTriggerCharacter = firstTriggerCharacter;
    }
}
