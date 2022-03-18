using System.Collections.Generic;
using System.Linq;

namespace Nezaboodka.Nevod.Services
{
    internal class SyntaxResolver : SyntaxVisitor
    {
        private int _offset;
        private Syntax? _result;

        internal Syntax? Resolve(PackageSyntax package, int offset)
        {
            _offset = offset;
            Visit(package);
            _offset = 0;
            Syntax? result = _result;
            _result = null;
            return result;
        }

        internal Syntax? ResolveFromChildren(IEnumerable<Syntax> children, int offset)
        {
            _offset = offset;
            Syntax? result = children.FirstOrDefault(child => IsOffsetInTextRange(child.TextRange));
            _offset = 0;
            return result;
        }

        public override Syntax? Visit(Syntax? node)
        {
            if (node is not null && IsOffsetInTextRange(node.TextRange))
            {
                _result = node;
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

        protected override Syntax VisitPatternReference(PatternReferenceSyntax node)
        {
            Visit(node.ExtractionFromFields);
            return node;
        }

        private bool IsOffsetInTextRange(TextRange range) => _offset >= range.Start && _offset < range.End;
    }
}
