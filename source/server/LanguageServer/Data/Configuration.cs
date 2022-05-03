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
        public bool InsertSpaceAfterOpeningAndBeforeClosingVariationBraces { get; set; }
        public bool InsertSpaceAfterOpeningAndBeforeClosingSpanBraces { get; set; }

        public static implicit operator Services.FormattingConfiguration(FormattingConfiguration configuration) =>
            new()
            {
                PlaceOpenBraceOnNewLine = configuration.PlaceOpenBraceOnNewLine,
                InsertSpaceAfterOpeningAndBeforeClosingVariationBraces = 
                    configuration.InsertSpaceAfterOpeningAndBeforeClosingVariationBraces,
                InsertSpaceAfterOpeningAndBeforeClosingSpanBraces = 
                    configuration.InsertSpaceAfterOpeningAndBeforeClosingSpanBraces,
            };
    }
}
