using System;
using System.Collections.Generic;

namespace Nezaboodka.Nevod.Services
{
    internal class PatternReferenceFinder : SyntaxVisitor
    {
        private PatternSyntax _syntax = null!; // Initialized in FindPatternReferences
        private SyntaxInfoTable _syntaxInfoTable = null!; // Initialized in FindPatternReferences
        private IReadOnlyDictionary<Uri, Document> _documents = null!; // Initialized in FindPatternReferences
        private List<Location> _references = null!;

        internal Location[] FindPatternReferences(PatternSyntax syntax, SyntaxInfoTable syntaxInfoTable,
            IReadOnlyDictionary<Uri, Document> documents, params LinkedPackageSyntax[] packages)
        {
            _syntax = syntax;
            _syntaxInfoTable = syntaxInfoTable;
            _documents = documents;
            _references = new List<Location>();
            foreach (LinkedPackageSyntax package in packages)
                Visit(package);
            _syntax = null!;
            _syntaxInfoTable = null!;
            _documents = null!;
            Location[] result = _references.ToArray();
            _references = null!;
            return result;
        }

        protected override Syntax VisitLinkedPackage(LinkedPackageSyntax node)
        {
            Visit(node.Patterns);
            Visit(node.SearchTargets);
            return node;
        }

        protected override Syntax VisitPatternReference(PatternReferenceSyntax node)
        {
            if (node.ReferencedPattern == _syntax)
                _references.Add(Utils.GetPatternReferenceNameLocation(node, _syntaxInfoTable, _documents));
            return node;
        }

        protected override Syntax VisitPatternSearchTarget(PatternSearchTargetSyntax node)
        {
            Visit(node.PatternReference);
            return node;
        }
    }
}
