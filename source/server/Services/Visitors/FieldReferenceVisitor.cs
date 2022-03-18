using System.Linq;

namespace Nezaboodka.Nevod.Services
{
    internal abstract class FieldReferenceVisitor : SyntaxVisitor
    {
        private FieldSyntax _field = null!; // Initialized in Visit
        private bool _isInFieldPattern;

        protected void Visit(FieldSyntax field, params LinkedPackageSyntax[] packages)
        {
            _field = field;
            foreach (LinkedPackageSyntax package in packages)
                Visit(package);
            _field = null!;
        }

        protected override Syntax VisitLinkedPackage(LinkedPackageSyntax node)
        {
            Visit(node.Patterns);
            Visit(node.SearchTargets);
            return node;
        }

        protected override Syntax VisitPattern(PatternSyntax node)
        {
            if (IsFieldDefinedInPattern(node))
            {
                _isInFieldPattern = true;
                Visit(node.Body);
                _isInFieldPattern = false;
            }
            else
                base.VisitPattern(node);
            return node;
        }

        protected override Syntax VisitPatternReference(PatternReferenceSyntax node)
        {
            if (node.ReferencedPattern is not null && IsFieldDefinedInPattern(node.ReferencedPattern))
            {
                Syntax? extraction =
                    node.ExtractionFromFields.FirstOrDefault(e => ((ExtractionFromFieldSyntax)e).FromFieldName == _field.Name);
                if (extraction is ExtractionFromFieldSyntax extractionFromField)
                    HandleExtractedField(extractionFromField);
            }
            else if (_isInFieldPattern)
            {
                Syntax? extraction =
                    node.ExtractionFromFields.FirstOrDefault(e => ((ExtractionFromFieldSyntax)e).FieldName == _field.Name);
                if (extraction is ExtractionFromFieldSyntax extractionFromField)
                    HandleExtractionField(extractionFromField);
            }
            return node;
        }

        protected override Syntax VisitFieldReference(FieldReferenceSyntax node)
        {
            if (_isInFieldPattern && node.FieldName == _field.Name)
                HandleFieldReference(node);
            return node;
        }

        protected override Syntax VisitExtraction(ExtractionSyntax node)
        {
            if (_isInFieldPattern && node.FieldName == _field.Name)
                HandleExtraction(node);
            return node;
        }

        private protected virtual void HandleExtraction(ExtractionSyntax extraction)
        {
        }

        private protected virtual void HandleExtractionField(ExtractionFromFieldSyntax extractionFromField)
        {
        }

        private protected virtual void HandleExtractedField(ExtractionFromFieldSyntax extractionFromField)
        {
        }

        private protected virtual void HandleFieldReference(FieldReferenceSyntax fieldReference)
        {
        }

        private bool IsFieldDefinedInPattern(PatternSyntax pattern) => pattern.Fields.Contains(_field);
    }
}
