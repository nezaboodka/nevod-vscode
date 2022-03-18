namespace Nezaboodka.Nevod.LanguageServer
{
    public class ShowMessageRequestClientCapabilities
    {
        public class MessageActionItemClientCapabilities
        {
            public bool? AdditionalPropertiesSupport { get; set; }
        }

        public MessageActionItemClientCapabilities? MessageActionItem { get; set; }
    }

    public class ShowDocumentClientCapabilities
    {
        public bool Support { get; set; }

        public ShowDocumentClientCapabilities(bool support) => Support = support;
    }
}
