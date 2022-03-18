namespace Nezaboodka.Nevod.Services
{
    public readonly struct TextEdit
    {
        public Location Location { get; }
        public string NewText { get; }

        public TextEdit(Location location, string newText)
        {
            Location = location;
            NewText = newText;
        }

        public override string ToString()
        {
            return $"{nameof(Location)}: {Location}, {nameof(NewText)}: {NewText}";
        }
    }
}
