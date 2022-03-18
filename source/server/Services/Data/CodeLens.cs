namespace Nezaboodka.Nevod.Services
{
    public readonly struct CodeLens
    {
        public Range Range { get; }
        public Range ActiveRange { get; }

        internal CodeLens(Range range, Range activeRange)
        {
            Range = range;
            ActiveRange = activeRange;
        }
    }
}
