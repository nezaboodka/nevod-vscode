namespace Nezaboodka.Nevod.Services
{
    public class Configuration
    {
        public FormattingConfiguration Formatting { get; }

        public Configuration(FormattingConfiguration formatting)
        {
            Formatting = formatting;
        }
    }

    public class FormattingConfiguration
    {
        public bool PlaceOpenBraceOnNewLine { get; set; }
    }
}
