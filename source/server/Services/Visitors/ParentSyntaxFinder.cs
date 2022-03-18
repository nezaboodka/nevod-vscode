namespace Nezaboodka.Nevod.Services
{
    internal class ParentSyntaxFinder : SyntaxVisitor
    {
        private int _offset;
        private Syntax _syntax = null!; // Initialized in FindParentPattern
        private PatternSyntax? _currentPattern;
        private Syntax? _currenSyntax;

        internal Syntax? FindParentSyntax(Syntax root, Syntax syntax)
        {
            _offset = syntax.TextRange.Start;
            _syntax = syntax;
            Visit(root);
            Syntax? result = _currenSyntax;
            _offset = 0;
            _syntax = null!;
            _currentPattern = null;
            return result;
        }

        internal PatternSyntax? FindParentPattern(Syntax root, Syntax syntax)
        {
            _offset = syntax.TextRange.Start;
            _syntax = syntax;
            Visit(root);
            PatternSyntax? result = _currentPattern;
            _offset = 0;
            _syntax = null!;
            _currentPattern = null;
            return result;
        }

        public override Syntax? Visit(Syntax? node)
        {
            if (node is not null && !ReferenceEquals(node, _syntax) && IsOffsetInTextRange(node.TextRange))
            {
                _currenSyntax = node;
                base.Visit(node);
            }
            return node;
        }

        protected override Syntax VisitRequiredPackage(RequiredPackageSyntax node) => node;

        protected override Syntax VisitPackage(PackageSyntax node)
        {
            Visit(node.RequiredPackages);
            Visit(node.Patterns);
            Visit(node.SearchTargets);
            return node;
        }

        protected override Syntax VisitLinkedPackage(LinkedPackageSyntax node) => VisitPackage(node);

        protected override Syntax VisitPatternSearchTarget(PatternSearchTargetSyntax node)
        {
            Visit(node.PatternReference);
            return node;
        }

        protected override Syntax VisitPattern(PatternSyntax node)
        {
            _currentPattern = node;
            return base.VisitPattern(node);
        }

        protected override Syntax VisitPatternReference(PatternReferenceSyntax node)
        {
            Visit(node.ExtractionFromFields);
            return node;
        }

        protected override Syntax VisitSequence(SequenceSyntax node)
        {
            Visit(node.Elements);
            return node;
        }

        private bool IsOffsetInTextRange(TextRange range) => _offset >= range.Start && _offset < range.End;
    }
}
