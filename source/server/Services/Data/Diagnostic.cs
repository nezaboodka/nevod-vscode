namespace Nezaboodka.Nevod.Services
{
    public readonly struct Diagnostic
    {
        public Range Range { get; }
        public string Message { get; }

        public Diagnostic(Range range, string message)
        {
            Range = range;
            Message = message;
        }
    }
}
