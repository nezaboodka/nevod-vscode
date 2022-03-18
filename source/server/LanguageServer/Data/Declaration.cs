namespace Nezaboodka.Nevod.LanguageServer
{
    public class DeclarationClientCapabilities
    {
        public bool? DynamicRegistration { get; set; }
        public bool LinkSupport { get; set; }

        public DeclarationClientCapabilities(bool linkSupport) => LinkSupport = linkSupport;
    }
}
