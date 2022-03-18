using System;
using System.Collections.Generic;

namespace Nezaboodka.Nevod.Services
{
    internal class SymbolFinder : SyntaxVisitor
    {
        private SyntaxInfoTable _syntaxInfoTable = null!; // Initialized in FindSymbols
        private IReadOnlyDictionary<Uri, Document> _documents = null!; // Initialized in FindSymbols
        private List<Symbol> _children = null!; // Initialized in FindSymbol

        internal Symbol[] FindSymbols(SyntaxInfoTable syntaxInfoTable,
            IReadOnlyDictionary<Uri, Document> documents, params LinkedPackageSyntax[] packages)
        {
            _syntaxInfoTable = syntaxInfoTable;
            _documents = documents;
            _children = new List<Symbol>();
            foreach (LinkedPackageSyntax package in packages)
                Visit(package);
            Symbol[] result = _children.ToArray();
            _syntaxInfoTable = null!;
            _documents = null!;
            _children = null!;
            return result;
        }

        protected override Syntax VisitLinkedPackage(LinkedPackageSyntax node)
        {
            Visit(node.Patterns);
            return node;
        }

        protected override Syntax VisitPattern(PatternSyntax node)
        {
            List<Symbol> parentChildren = _children;
            _children = new List<Symbol>();
            Visit(node.NestedPatterns);
            if (node.Name != null)
            {
                Symbol symbol = CreateSymbol(node, _children);
                parentChildren.Add(symbol);
            }
            else
                parentChildren.AddRange(_children);
            _children = parentChildren;
            return node;
        }

        private Symbol CreateSymbol(PatternSyntax pattern, IEnumerable<Symbol>? children)
        {
            PatternSyntaxInfo info = _syntaxInfoTable.GetSyntaxInfo(pattern);
            Location location = Utils.TrimLocation(info.Location, _documents);
            Location nameLocation = Utils.GetPatternNameLocation(info.Syntax, _syntaxInfoTable, _documents);
            return new Symbol(pattern.Name!, pattern.FullName, children, location, nameLocation);
        }
    }
}
