using System;
using System.Collections.Generic;

namespace Nezaboodka.Nevod.Services
{
    internal class FieldReferenceFinder : FieldReferenceVisitor
    {
        private SyntaxInfoTable _syntaxInfoTable = null!; // Initialized in FindFieldReferences
        private IReadOnlyDictionary<Uri, Document> _documents = null!; // Initialized in FindFieldReferences
        private List<Location> _references = null!;

        internal Location[] FindFieldReferences(FieldSyntax field, SyntaxInfoTable syntaxInfoTable,
            IReadOnlyDictionary<Uri, Document> documents, params LinkedPackageSyntax[] packages)
        {
            _syntaxInfoTable = syntaxInfoTable;
            _documents = documents;
            _references = new List<Location>();
            Visit(field, packages);
            _syntaxInfoTable =  null!;
            _documents =  null!;
            Location[] result = _references.ToArray();
            _references = null!;
            return result;
        }

        private protected override void HandleExtraction(ExtractionSyntax extraction)
        {
            _references.Add(Utils.GetExtractionLocation(extraction, _syntaxInfoTable, _documents));
        }

        private protected override void HandleExtractionField(ExtractionFromFieldSyntax extractionFromField)
        {
            _references.Add(Utils.GetExtractionLocation(extractionFromField, _syntaxInfoTable, _documents));
        }

        private protected override void HandleExtractedField(ExtractionFromFieldSyntax extractionFromField)
        {
            _references.Add(Utils.GetFromFieldLocation(extractionFromField, _syntaxInfoTable, _documents));
        }

        private protected override void HandleFieldReference(FieldReferenceSyntax fieldReference)
        {
            _references.Add(Utils.FetFieldReferenceLocation(fieldReference, _syntaxInfoTable, _documents));
        }
    }
}
