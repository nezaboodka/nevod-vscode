namespace Nezaboodka.Nevod.LanguageServer
{
    public class Configuration
    {
        public FormattingConfiguration Formatting { get; set; }

        public Configuration(FormattingConfiguration formatting)
        {
            Formatting = formatting;
        }

        public static implicit operator Services.Configuration(Configuration configuration) =>
            new(configuration.Formatting);
    }

    public class FormattingConfiguration
    {
        public bool PlaceOpenBraceOnNewLine { get; set; }

        public static implicit operator Services.FormattingConfiguration(FormattingConfiguration configuration) =>
            new()
            {
                PlaceOpenBraceOnNewLine = configuration.PlaceOpenBraceOnNewLine
            };
    }
}
