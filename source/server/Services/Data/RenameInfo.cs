namespace Nezaboodka.Nevod.Services
{
    public readonly struct RenameInfo
    {
        public Range Range { get; }
        public string Text { get; }

        public RenameInfo(Range range, string text)
        {
            Range = range;
            Text = text;
        }
    }
}
