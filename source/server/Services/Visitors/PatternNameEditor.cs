using System;
using System.Collections.Generic;

namespace Nezaboodka.Nevod.Services
{
    internal class PatternNameEditor : SyntaxVisitor
    {
        private PatternSyntax _renamedPatternSyntax = null!; // Initialized in CreateTextEdits
        private string _newName = null!; // Initialized in CreateTextEdits
        private SyntaxInfoTable _syntaxInfoTable = null!; // Initialized in CreateTextEdits
        private IReadOnlyDictionary<Uri, Document> _documents = null!; // Initialized in CreateTextEdits
        private List<TextEdit> _textEdits = null!; // Initialized in CreateTextEdits
        private bool _isFirstPass;
        private Dictionary<PatternSyntax, string> _relativeNameByNestedPattern = null!; // Initialized in CreateTextEdits
        private string _relativeNamePart = null!;

        internal TextEdit[] CreateTextEdits(PatternSyntax patternSyntax, string newName,
            SyntaxInfoTable syntaxInfoTable, IReadOnlyDictionary<Uri, Document> documents, params LinkedPackageSyntax[] packages)
        {
            if (patternSyntax.Name is null)
                throw new Exception("Text edits cannot be created for pattern without name");
            _renamedPatternSyntax = patternSyntax;
            _textEdits = new List<TextEdit>();
            _newName = newName;
            _syntaxInfoTable = syntaxInfoTable;
            _documents = documents;
            _isFirstPass = true;
            _relativeNameByNestedPattern = new Dictionary<PatternSyntax, string>();
            _relativeNamePart = patternSyntax.Name;
            Visit(patternSyntax.NestedPatterns);
            _relativeNamePart = null!;
            _isFirstPass = false;
            foreach (LinkedPackageSyntax package in packages)
                Visit(package);
            _textEdits.Add(CreateTextEdit(patternSyntax, newName));
            TextEdit[] result = _textEdits.ToArray();
            _newName = null!;
            _syntaxInfoTable = null!;
            _documents = null!;
            _renamedPatternSyntax = null!;
            _textEdits = null!;
            _relativeNameByNestedPattern = null!;
            return result;
        }

        protected override Syntax VisitLinkedPackage(LinkedPackageSyntax node)
        {
            Visit(node.Patterns);
            Visit(node.SearchTargets);
            return node;
        }

        protected override Syntax VisitPattern(PatternSyntax node)
        {
            if (_isFirstPass)
            {
                string saveRelativeNamePart = _relativeNamePart;
                _relativeNamePart = Syntax.GetFullName(_relativeNamePart, node.Name);
                _relativeNameByNestedPattern[node] = _relativeNamePart;
                Visit(node.NestedPatterns);
                _relativeNamePart = saveRelativeNamePart;
            }
            else
            {
                Visit(node.Body);
                Visit(node.NestedPatterns);
            }
            return node;
        }

        protected override Syntax VisitPatternReference(PatternReferenceSyntax node)
        {
            if (!_isFirstPass)
                if (node.ReferencedPattern == _renamedPatternSyntax)
                {
                    int shortNameStart = node.PatternName.LastIndexOf(_renamedPatternSyntax.Name!, StringComparison.Ordinal);
                    string newLongName = node.PatternName[..shortNameStart] + _newName;
                    _textEdits.Add(CreateTextEdit(node, newLongName));
                }
                else if (node.ReferencedPattern is not null &&
                         _relativeNameByNestedPattern.TryGetValue(node.ReferencedPattern, out string? relativeName))
                {
                    int relativeNameStart = node.PatternName.LastIndexOf(relativeName, StringComparison.Ordinal);
                    // Reference does not contain relative name. It means that nested pattern is referenced from nested
                    // patterns of _renamedPatternSyntax or its body and thus does not need to be renamed.
                    if (relativeNameStart != -1)
                    {
                        int nestedNameStart = relativeNameStart + _renamedPatternSyntax.Name!.Length;
                        string newLongName = node.PatternName[..relativeNameStart] + _newName + node.PatternName[nestedNameStart..];
                        _textEdits.Add(CreateTextEdit(node, newLongName));
                    }
                }
            return node;
        }

        protected override Syntax VisitPatternSearchTarget(PatternSearchTargetSyntax node)
        {
            Visit(node.PatternReference);
            return node;
        }

        private TextEdit CreateTextEdit(Syntax syntax, string newName)
        {
            Location oldTextLocation = syntax switch
            {
                PatternReferenceSyntax patternReferenceSyntax => 
                    Utils.GetPatternReferenceNameLocation(patternReferenceSyntax, _syntaxInfoTable, _documents),
                PatternSyntax patternSyntax => Utils.GetPatternNameLocation(patternSyntax, _syntaxInfoTable, _documents),
                _ => throw new ArgumentOutOfRangeException(nameof(syntax))
            };
            return new TextEdit(oldTextLocation, newName);
        }
    }
}
