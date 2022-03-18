namespace Nezaboodka.Nevod.Services
{
    public readonly struct Range
    {
        public Position Start { get; }
        public Position End { get; }
        public bool Empty => Start.Equals(End);

        public Range(Position start, Position end)
        {
            Start = start;
            End = end;
        }

        public override string ToString() => $"{Start}, {End}";
    }
}
