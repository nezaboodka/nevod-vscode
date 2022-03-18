using System;

namespace Nezaboodka.Nevod.Services
{
    public readonly struct Position
    {
        public readonly int Line;
        public readonly int Character;

        public Position(int line, int character)
        {
            Line = line;
            Character = character;
        }

        public override bool Equals(object? obj)
        {
            if (obj is Position position)
                return Line.Equals(position.Line) && Character.Equals(position.Character);
            else
                return false;
        }

        public override int GetHashCode() => HashCode.Combine(Line, Character);

        public override string ToString() => $"{Line}:{Character}";
    }
}
