using System;
using System.Collections.Generic;

namespace Nezaboodka.Nevod.Services
{
    internal class RenameProvider
    {
        private readonly PatternNameEditor _patternNameEditor = new();
        private readonly FieldNameEditor _fieldNameEditor = new();
        private SyntaxInfoTable _syntaxInfoTable = null!; // Initialized in GetCompletions
        private IReadOnlyDictionary<Uri, Document> _documents = null!; // Initialized in GetCompletions

        internal RenameInfo? GetRenameInfo(PointerLocation location, 
            SyntaxInfoTable syntaxInfoTable, IReadOnlyDictionary<Uri, Document> documents)
        {
            _syntaxInfoTable = syntaxInfoTable;
            _documents = documents;
            Lexeme? lexeme = Utils.GetPointedLexeme(location, out Syntax? parent, 
                _syntaxInfoTable, _documents, checkForPreviousLexeme: true, returnPreviousIfInEndOfFile: true);
            RenameInfo? result = null;
            if (lexeme is not null)
            {
                Document document = _documents[location.Uri];
                Location lexemeLocation = Utils.GetLexemeLocation(lexeme, document);
                if (lexemeLocation.Contains(location) &&
                    parent is not null &&
                    LexemeCanBeRenamed(lexeme, parent, document.Uri, out RenameInfo renameInfo, out _))
                    result = renameInfo;
            }
            _syntaxInfoTable = null!;
            _documents = null!;
            return result;
        }

        internal IEnumerable<TextEdit>? RenameSymbol(PointerLocation location, string newName,
            SyntaxInfoTable syntaxInfoTable, IReadOnlyDictionary<Uri, Document> documents)
        {
            _syntaxInfoTable = syntaxInfoTable;
            _documents = documents;
            Lexeme? lexeme = Utils.GetPointedLexeme(location, out Syntax? parent, 
                _syntaxInfoTable, _documents, checkForPreviousLexeme: true, returnPreviousIfInEndOfFile: true);
            IEnumerable<TextEdit>? result = null;
            if (lexeme is not null)
            {
                Document document = _documents[location.Uri];
                Location lexemeLocation = Utils.GetLexemeLocation(lexeme, document);
                if (lexemeLocation.Contains(location) &&
                    parent is not null &&
                    LexemeCanBeRenamed(lexeme, parent, document.Uri, out _, out Syntax? definition))
                {
                    definition ??= parent;
                    result = definition switch
                    {
                        PatternSyntax pattern => _patternNameEditor.CreateTextEdits(pattern, newName, _syntaxInfoTable,
                            _documents, Utils.GetAllLinkedPackages(_syntaxInfoTable, _documents)),
                        FieldSyntax field => _fieldNameEditor.CreateTextEdits(field, newName, _syntaxInfoTable,
                            _documents, Utils.GetAllLinkedPackages(_syntaxInfoTable, _documents)),
                        _ => result
                    };
                }
            }
            _syntaxInfoTable = null!;
            _documents = null!;
            return result;
        }

        private bool LexemeCanBeRenamed(Lexeme lexeme, Syntax parent, Uri documentUri, 
            out RenameInfo renameInfo, out Syntax? definition)
        {
            renameInfo = new RenameInfo();
            definition = null;                       
            if (lexeme.TokenId is TokenId.Identifier)
            {
                Document document = _documents[documentUri];
                Range lexemeRange = Utils.GetLexemeLocation(lexeme, document).Range;
                switch (parent)
                {
                    case PatternSyntax pattern:
                        if (pattern.Name is not null)
                            renameInfo = new RenameInfo(lexemeRange, pattern.Name);
                        return true;
                    case PatternReferenceSyntax patternReference:
                        if (patternReference.ReferencedPattern?.Name != null)
                        {
                            renameInfo = new RenameInfo(lexemeRange, patternReference.ReferencedPattern.Name);
                            definition = patternReference.ReferencedPattern;
                            return true;
                        }
                        break;
                    case FieldSyntax field:
                        renameInfo = new RenameInfo(lexemeRange, field.Name);
                        return true;
                    case ExtractionSyntax extraction:
                        ExtractionSyntaxInfo extractionInfo = _syntaxInfoTable.GetSyntaxInfo(extraction);
                        ISyntaxInfo<FieldSyntax>? fieldDefinitionForExtraction = Utils.GetFieldDefinition(
                            extractionInfo.Pattern,
                            extraction.FieldName,
                            _syntaxInfoTable);
                        if (fieldDefinitionForExtraction is not null)
                        {
                            renameInfo = new RenameInfo(lexemeRange, extraction.FieldName);
                            definition = fieldDefinitionForExtraction.Syntax;
                            return true;
                        }
                        break;
                    case FieldReferenceSyntax fieldReference:
                        FieldReferenceSyntaxInfo fieldReferenceInfo = _syntaxInfoTable.GetSyntaxInfo(fieldReference);
                        ISyntaxInfo<FieldSyntax>? fieldDefinitionForReference = Utils.GetFieldDefinition(
                            fieldReferenceInfo.Pattern,
                            fieldReference.FieldName,
                            _syntaxInfoTable);
                        if (fieldDefinitionForReference is not null)
                        {
                            renameInfo = new RenameInfo(lexemeRange, fieldReference.FieldName);
                            definition = fieldDefinitionForReference.Syntax;
                            return true;
                        }
                        break;
                    case ExtractionFromFieldSyntax extractionFromField:
                        ExtractionFromFieldSyntaxInfo extractionFromFieldInfo = _syntaxInfoTable.GetSyntaxInfo(extractionFromField);
                        // Pointed lexeme is field name
                        if (ReferenceEquals(extractionFromField.Children[0], lexeme))
                        {
                            ISyntaxInfo<FieldSyntax>? fieldInfo = Utils.GetFieldDefinition(
                                extractionFromFieldInfo.Pattern,
                                extractionFromField.FieldName,
                                _syntaxInfoTable);
                            if (fieldInfo is not null)
                            {
                                renameInfo = new RenameInfo(lexemeRange, extractionFromField.FieldName);
                                definition = fieldInfo.Syntax;
                                return true;
                            }
                        }
                        // Pointed lexeme is from field name
                        else if (extractionFromField.FromFieldName is not null &&
                                 extractionFromFieldInfo.PatternReference.ReferencedPattern is not null)
                        {
                            ISyntaxInfo<FieldSyntax>? fieldInfo = Utils.GetFieldDefinition(
                                extractionFromFieldInfo.PatternReference.ReferencedPattern,
                                extractionFromField.FromFieldName,
                                _syntaxInfoTable);
                            if (fieldInfo is not null)
                            {
                                renameInfo = new RenameInfo(lexemeRange, extractionFromField.FromFieldName);
                                definition = fieldInfo.Syntax;
                                return true;
                            }
                        }
                        break;
                }
            }
            return false;
        }
    }
}
