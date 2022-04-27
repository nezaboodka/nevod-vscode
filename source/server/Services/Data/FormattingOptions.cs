namespace Nezaboodka.Nevod.Services
{
    public class FormattingOptions
    {
        public uint TabSize { get; }
        public bool InsertSpaces { get; }
        public string? NewLine { get; }

        public FormattingOptions(uint tabSize, bool insertSpaces, string? newLine)
        {
            TabSize = tabSize;
            NewLine = newLine;
            InsertSpaces = insertSpaces;
        }
    }
}
