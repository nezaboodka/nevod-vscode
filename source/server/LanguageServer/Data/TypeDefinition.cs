namespace Nezaboodka.Nevod.LanguageServer
{
    public class TypeDefinitionClientCapabilities
    {
        public bool? DynamicRegistration { get; set; }
        public bool LinkSupport { get; set; }

        public TypeDefinitionClientCapabilities(bool linkSupport) => LinkSupport = linkSupport;
    }
}
