using System.Collections.Generic;

namespace Nezaboodka.Nevod.Services
{
    internal class PatternFinder : SyntaxVisitor
    {
        private List<PatternSyntax> _patterns = null!; // Initialized in FindPatterns

        internal PatternSyntax[] FindPatterns(params LinkedPackageSyntax[] packages)
        {
            _patterns = new List<PatternSyntax>();
            foreach (LinkedPackageSyntax package in packages)
                Visit(package);
            PatternSyntax[] result = _patterns.ToArray();
            _patterns = null!;
            return result;
        }

        protected override Syntax VisitLinkedPackage(LinkedPackageSyntax node)
        {
            Visit(node.Patterns);
            return node;
        }

        protected override Syntax VisitPattern(PatternSyntax node)
        {
            _patterns.Add(node);
            Visit(node.NestedPatterns);
            return node;
        }
    }
}
