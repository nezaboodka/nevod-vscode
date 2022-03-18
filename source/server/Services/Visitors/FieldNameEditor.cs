using System;
using System.Collections.Generic;

namespace Nezaboodka.Nevod.Services
{
    internal class FieldNameEditor : FieldReferenceVisitor
    {
        private string _newName = null!; // Initialized in CreateTextEdits
        private SyntaxInfoTable _syntaxInfoTable = null!; // Initialized in CreateTextEdits
        private IReadOnlyDictionary<Uri, Document> _documents = null!; // Initialized in CreateTextEdits
        private List<TextEdit> _textEdits = null!; // Initialized in CreateTextEdits

        internal TextEdit[] CreateTextEdits(FieldSyntax field, string newName, 
            SyntaxInfoTable syntaxInfoTable, IReadOnlyDictionary<Uri, Document> documents, params LinkedPackageSyntax[] packages)
        {
            _newName = newName;
            _syntaxInfoTable = syntaxInfoTable;
            _documents = documents;
            _textEdits = new List<TextEdit>();
            Visit(field, packages);
            _textEdits.Add(CreateFieldTextEdit(field));
            _newName = null!;
            _syntaxInfoTable = null!;
            _documents = null!;
            TextEdit[] result = _textEdits.ToArray();
            _textEdits = null!;
            return result;
        }

        private protected override void HandleExtraction(ExtractionSyntax extraction)
        {
            Location oldTextLocation = Utils.GetExtractionLocation(extraction, _syntaxInfoTable, _documents);
            _textEdits.Add(new TextEdit(oldTextLocation, _newName));
        }

        private protected override void HandleExtractionField(ExtractionFromFieldSyntax extractionFromField)
        {
            Location oldTextLocation = Utils.GetExtractionLocation(extractionFromField, _syntaxInfoTable, _documents);
            _textEdits.Add(new TextEdit(oldTextLocation, _newName));
        }

        private protected override void HandleExtractedField(ExtractionFromFieldSyntax extractionFromField)
        {
            Location oldTextLocation = Utils.GetFromFieldLocation(extractionFromField, _syntaxInfoTable, _documents);
            _textEdits.Add(new TextEdit(oldTextLocation, _newName));
        }

        private protected override void HandleFieldReference(FieldReferenceSyntax fieldReference)
        {
            Location oldTextLocation = Utils.FetFieldReferenceLocation(fieldReference, _syntaxInfoTable, _documents);
            _textEdits.Add(new TextEdit(oldTextLocation, _newName));
        }

        private TextEdit CreateFieldTextEdit(FieldSyntax field)
        {
            Location oldTextLocation = Utils.TrimLocation(_syntaxInfoTable.GetSyntaxInfo(field).Location, _documents);
            return new TextEdit(oldTextLocation, _newName);
        }
    }
}
