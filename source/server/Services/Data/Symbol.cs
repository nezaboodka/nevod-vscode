using System;
using System.Collections.Generic;
using System.Linq;

namespace Nezaboodka.Nevod.Services
{
    public readonly struct Symbol
    {
        public string Name { get; }
        public string Detail { get; }
        public IEnumerable<Symbol>? Children { get; }
        public Location Location { get; }
        public Location NameLocation { get; }

        internal Symbol(string name, string detail, IEnumerable<Symbol>? children, Location location, Location nameLocation)
        {
            Name = name;
            Detail = detail;
            Children = children;
            Location = location;
            NameLocation = nameLocation;
        }

        public override bool Equals(object? obj)
        {
            if (obj is Symbol symbol)
            {
                if (Children is null && symbol.Children is not null || Children is not null && symbol.Children is null)
                    return false;
                return Name == symbol.Name
                       && Detail == symbol.Detail
                       && (Children is null && symbol.Children is null || Children!.SequenceEqual(symbol.Children!)) // Not null values are guaranteed by previous checks
                       && Location.Equals(symbol.Location)
                       && NameLocation.Equals(symbol.NameLocation);
            }
            else
                return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Detail, Children, Location, NameLocation);
        }
    }
}
