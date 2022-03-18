using System;

namespace Nezaboodka.Nevod.Services
{
    public class Location
    {
        public Uri Uri { get; }
        public Range Range { get; }

        public Location(Uri uri, Range range)
        {
            Uri = uri;
            Range = range;
        }

        public bool Contains(Location location)
        {
            if (Uri != location.Uri)
                return false;
            Position start = Range.Start;
            Position end = Range.End;
            Position otherStart = location.Range.Start;
            Position otherEnd = location.Range.End;
            if (otherStart.Line < start.Line || otherEnd.Line > end.Line)
                return false;
            if (otherStart.Line == start.Line && otherStart.Character < start.Character)
                return false;
            if (otherEnd.Line == end.Line && otherEnd.Character > end.Character)
                return false;
            return true;
        }

        public bool ContainsExcludingEdges(Location location)
        {
            if (Uri != location.Uri)
                return false;
            Position start = Range.Start;
            Position end = Range.End;
            Position otherStart = location.Range.Start;
            Position otherEnd = location.Range.End;
            if (otherStart.Line < start.Line || otherEnd.Line > end.Line)
                return false;
            if (otherStart.Line == start.Line && otherStart.Character <= start.Character)
                return false;
            if (otherEnd.Line == end.Line && otherEnd.Character >= end.Character)
                return false;
            return true;
        }

        public override bool Equals(object? obj) => 
            obj is Location location && Uri == location.Uri && Equals(Range, location.Range);

        public override int GetHashCode() => HashCode.Combine(Uri, Range);

        public override string ToString() => $"{nameof(Uri)}: {Uri}, {nameof(Range)}: {Range}";
    }

    public class PointerLocation : Location
    {
        public Position Position { get; }

        public PointerLocation(Uri uri, Position position) : base(uri, new Range(position, position)) => Position = position;
    }
}
