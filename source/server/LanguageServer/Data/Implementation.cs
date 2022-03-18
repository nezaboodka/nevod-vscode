namespace Nezaboodka.Nevod.LanguageServer
{
    public class ImplementationClientCapabilities
    {
        public bool? DynamicRegistration { get; set; }
        public bool LinkSupport { get; set; }

        public ImplementationClientCapabilities(bool linkSupport) => LinkSupport = linkSupport;
    }
}
