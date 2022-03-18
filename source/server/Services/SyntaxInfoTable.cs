using System;
using System.Collections.Generic;

namespace Nezaboodka.Nevod.Services
{
    internal class SyntaxInfoTable
    {
        private readonly Dictionary<Syntax, ISyntaxInfo<Syntax>> _items;
        private readonly Dictionary<Uri, SyntaxInfo<LinkedPackageSyntax>> _linkedPackages = new();

        internal SyntaxInfoTable(Dictionary<Syntax, ISyntaxInfo<Syntax>> items)
        {
            _items = items;
            foreach (ISyntaxInfo<Syntax> info in items.Values)
                if (info is SyntaxInfo<LinkedPackageSyntax> syntaxInfo)
                    _linkedPackages.Add(syntaxInfo.Location.Uri, syntaxInfo);
        }

        internal ISyntaxInfo<TSyntax> GetSyntaxInfo<TSyntax>(TSyntax syntax) where TSyntax : Syntax =>
            (ISyntaxInfo<TSyntax>)_items[syntax];

        internal PatternSyntaxInfo GetSyntaxInfo(PatternSyntax syntax) => (PatternSyntaxInfo)_items[syntax];

        internal ExtractionSyntaxInfo GetSyntaxInfo(ExtractionSyntax syntax) => (ExtractionSyntaxInfo)_items[syntax];

        internal FieldReferenceSyntaxInfo GetSyntaxInfo(FieldReferenceSyntax syntax) => 
            (FieldReferenceSyntaxInfo)_items[syntax];

        internal ExtractionFromFieldSyntaxInfo GetSyntaxInfo(ExtractionFromFieldSyntax syntax) => 
            (ExtractionFromFieldSyntaxInfo)_items[syntax];

        internal SyntaxInfo<LinkedPackageSyntax> GetLinkedPackageSyntaxInfo(Uri uri) => _linkedPackages[uri];
    }

    internal interface ISyntaxInfo<out TSyntax> where TSyntax : Syntax
    {
        TSyntax Syntax { get; }
        Location Location { get; }
    }

    internal class SyntaxInfo<TSyntax> : ISyntaxInfo<TSyntax> where TSyntax : Syntax
    {
        public TSyntax Syntax { get; }
        public Location Location { get; }

        internal SyntaxInfo(TSyntax syntax, Location location)
        {
            Syntax = syntax;
            Location = location;
        }
    }

    internal class PatternSyntaxInfo : SyntaxInfo<PatternSyntax>
    {
        internal PatternSyntax? MasterPattern { get; }

        internal PatternSyntaxInfo(PatternSyntax pattern, Location location, PatternSyntax? masterPattern = null) :
            base(pattern, location) => MasterPattern = masterPattern;
    }

    internal class ExtractionSyntaxInfo : SyntaxInfo<ExtractionSyntax>
    {
        internal PatternSyntax Pattern { get; }

        internal ExtractionSyntaxInfo(ExtractionSyntax extraction, Location location, PatternSyntax pattern) :
            base(extraction, location) => Pattern = pattern;
    }

    internal class FieldReferenceSyntaxInfo : SyntaxInfo<FieldReferenceSyntax>
    {
        internal PatternSyntax Pattern { get; }

        internal FieldReferenceSyntaxInfo(FieldReferenceSyntax fieldReference, Location location, PatternSyntax pattern) :
            base(fieldReference, location) => Pattern = pattern;
    }

    internal class ExtractionFromFieldSyntaxInfo : SyntaxInfo<ExtractionFromFieldSyntax>
    {
        internal PatternSyntax Pattern { get; }
        internal PatternReferenceSyntax PatternReference { get; }

        internal ExtractionFromFieldSyntaxInfo(ExtractionFromFieldSyntax extraction, Location location, PatternSyntax pattern, PatternReferenceSyntax patternReference) :
            base(extraction, location)
        {
            Pattern = pattern;
            PatternReference = patternReference;
        }
    }
}
