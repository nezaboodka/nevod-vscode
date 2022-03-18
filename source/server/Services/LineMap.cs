using System.Collections.Generic;

namespace Nezaboodka.Nevod.Services
{
    internal class LineMap
    {
        private readonly List<int> _lineStartIndices;

        internal LineMap(string text)
        {
            _lineStartIndices = new List<int>();
            for (var i = 0; i < text.Length; i++)
                if (text[i] == '\n')
                    _lineStartIndices.Add(i + 1);
        }

        // TODO: Check offset out of bounds
        internal Position PositionAt(int offset)
        {
            if (_lineStartIndices.Count == 0)
                return new Position(0, offset);
            int index = _lineStartIndices.BinarySearch(offset);
            if (index >= 0)
                return new Position(index + 1, 0);
            index = ~index;
            if (index == 0)
                return new Position(0, offset);
            return new Position(index, offset - _lineStartIndices[index - 1]);
        }

        internal int OffsetAt(Position position)
        {
            if (position.Line == 0 || _lineStartIndices.Count == 0)
                return position.Character;
            if (position.Line <= _lineStartIndices.Count)
                return _lineStartIndices[position.Line - 1] + position.Character;
            return _lineStartIndices[^1] + position.Character;
        }
    }
}
