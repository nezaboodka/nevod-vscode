using System;
using System.Collections.Generic;
using System.Linq;

namespace Nezaboodka.Nevod.Services
{
    internal static class Utils
    {
        internal static TextRange TrimStartSingleLineComment(string text, TextRange range)
        {
            ValidateTextRange(text, range);
            ReadOnlySpan<char> span = text.AsSpan(range.Start, range.Length);
            while (span.StartsWith("//"))
            {
                int lastCommentIndex = span.IndexOfAny('\r', '\n');
                if (lastCommentIndex == -1)
                    lastCommentIndex = span.Length - 1;
                if (lastCommentIndex + 1 < span.Length && span[lastCommentIndex] == '\r' && span[lastCommentIndex + 1] == '\n')
                    lastCommentIndex++;
                range = new TextRange(range.Start + lastCommentIndex + 1, range.End);
                span = text.AsSpan(range.Start, range.Length);
            }
            return range;
        }

        internal static TextRange TrimStartMultilineComment(string text, TextRange range)
        {
            ValidateTextRange(text, range);
            ReadOnlySpan<char> span = text.AsSpan(range.Start, range.Length);
            while (span.StartsWith("/*"))
            {
                int lastCommentIndex = span.IndexOf("*/", StringComparison.Ordinal);
                if (lastCommentIndex == -1)
                    range = new TextRange(range.End, range.End);
                else
                    range = new TextRange(range.Start + lastCommentIndex + 2, range.End);
                span = text.AsSpan(range.Start, range.Length);
            }
            return range;
        }

        internal static TextRange TrimStartTrivia(string text, TextRange range)
        {
            ValidateTextRange(text, range);
            TextRange rangeAfterPreviousTrim;
            do
            {
                rangeAfterPreviousTrim = range;
                range = TrimStartSingleLineComment(text, range);
                range = TrimStartMultilineComment(text, range);
                while (!range.IsEmpty && char.IsWhiteSpace(text[range.Start]))
                    range = new TextRange(range.Start + 1, range.End);
            } while (!Equals(range, rangeAfterPreviousTrim));
            return range;
        }

        internal static TextRange TrimStartString(string text, TextRange range)
        {
            ValidateTextRange(text, range);
            int currentIndex = range.Start;
            if (currentIndex < range.End && text[currentIndex] is var quote and ('"' or '\''))
            {
                while (currentIndex < range.End && text[currentIndex] == quote)
                {
                    do
                        currentIndex++;
                    while (currentIndex < range.End && text[currentIndex] != quote);
                    if (currentIndex < range.End && text[currentIndex] == quote)
                        currentIndex++;
                }
                if (currentIndex < range.End && text[currentIndex] is '!')
                    currentIndex++;
                if (currentIndex < range.End && text[currentIndex] is '*')
                    currentIndex++;
            }
            return new TextRange(currentIndex, range.End);
        }

        internal static TextRange TrimEndTrivia(string text, TextRange range)
        {
            ValidateTextRange(text, range);
            int currentIndex = range.Start;
            int lastNonTriviaIndex = range.Start - 1;
            while (currentIndex < range.End)
            {
                TextRange maybeWithStartString = new(currentIndex, range.End);
                TextRange maybeWithStartTrivia = TrimStartString(text, maybeWithStartString);
                if (!Equals(maybeWithStartString, maybeWithStartTrivia))
                    lastNonTriviaIndex = maybeWithStartTrivia.Start - 1;
                TextRange withoutStartTrivia = TrimStartTrivia(text, maybeWithStartTrivia);
                currentIndex = range.Start + range.Length - withoutStartTrivia.Length;
                if (!withoutStartTrivia.IsEmpty)
                    if (Equals(maybeWithStartTrivia, withoutStartTrivia)) // text did not start with trivia
                        lastNonTriviaIndex = currentIndex++;
                    else // text started with trivia which was not last
                        lastNonTriviaIndex = currentIndex;
            }
            return new TextRange(range.Start, lastNonTriviaIndex + 1);
        }

        internal static TextRange TrimStartKeywords(string text, TextRange range)
        {
            ValidateTextRange(text, range);
            TextRange rangeAfterPreviousTrim;
            do
            {
                rangeAfterPreviousTrim = range;
                range = TrimStartTrivia(text, range);
                if (text.AsSpan(range.Start, range.Length).StartsWith("@"))
                {
                    int i = range.Start + 1;
                    while (i < range.End && char.IsLetter(text[i]))
                        i++;
                    range = new TextRange(i, range.End);
                }
            } while (!Equals(range, rangeAfterPreviousTrim));
            return range;
        }

        internal static TextRange GetMultipartIdentifierRange(string text, TextRange range)
        {
            ValidateTextRange(text, range);
            int currentIndex = range.Start;
            while (currentIndex < range.End)
            {
                while (currentIndex < range.End && (char.IsLetterOrDigit(text[currentIndex]) || text[currentIndex] is '-'))
                    currentIndex++;
                TextRange maybeWithStartString = new(currentIndex, range.End);
                TextRange withoutStartTrivia = TrimStartTrivia(text, maybeWithStartString);
                if (withoutStartTrivia.Start < range.End && text[withoutStartTrivia.Start] is '.')
                {
                    currentIndex = withoutStartTrivia.Start + 1;
                    maybeWithStartString = new TextRange(currentIndex, range.End);
                    withoutStartTrivia = TrimStartTrivia(text, maybeWithStartString);
                    if (withoutStartTrivia.Start < range.End) 
                        if(text[withoutStartTrivia.Start] is '*')
                        {
                            currentIndex = withoutStartTrivia.Start + 1;
                            break;
                        }
                        else if (text[withoutStartTrivia.Start] is '.')
                            break;
                        else if (char.IsLetter(text[withoutStartTrivia.Start]))
                            currentIndex = withoutStartTrivia.Start;
                }
                else
                    break;
            }
            return new TextRange(range.Start, currentIndex);
        }

        internal static bool IsOffsetInComment(int offset, string text, TextRange range)
        {
            ValidateTextRange(text, range);
            bool isInComment = false;
            while (!isInComment && !range.IsEmpty && range.Start < offset)
            {
                ReadOnlySpan<char> span = text.AsSpan(range.Start, range.Length);
                if (span.StartsWith("//"))
                {
                    range = TrimStartSingleLineComment(text, range);
                    if (range.Start > offset)
                        isInComment = true;
                }
                else if (span.StartsWith("/*"))
                {
                    range = TrimStartMultilineComment(text, range);
                    if (range.Start > offset)
                        isInComment = true;
                }
                else if (span.StartsWith("\'") || span.StartsWith("'"))
                    range = TrimStartString(text, range);
                else
                    range = new TextRange(range.Start + 1, range.End);
            }
            return isInComment;
        }

        internal static Location TrimLocation(Location location, IReadOnlyDictionary<Uri, Document> documents)
        {
            Document document = documents[location.Uri];
            int start = document.OffsetAt(location.Range.Start);
            int end = document.OffsetAt(location.Range.End);
            TextRange range = new(start, end);
            range = TrimStartTrivia(document.Text, range);
            range = TrimEndTrivia(document.Text, range);
            Position startPosition = document.PositionAt(range.Start);
            Position endPosition = document.PositionAt(range.End);
            return new Location(location.Uri, new Range(startPosition, endPosition));
        }

        internal static Location TrimLocationWhitespaces(Location location, IReadOnlyDictionary<Uri, Document> documents)
        {
            Document document = documents[location.Uri];
            int start = document.OffsetAt(location.Range.Start);
            int end = document.OffsetAt(location.Range.End);
            string text = document.Text;
            while (start < end && start < text.Length && char.IsWhiteSpace(text[start]))
                start++;
            while (end > start && char.IsWhiteSpace(text[end]))
                end++;
            Position lexemeStart = document.PositionAt(start);
            Position lexemeEnd = document.PositionAt(end);
            return new Location(document.Uri, new Range(lexemeStart, lexemeEnd));
        }

        internal static Location GetPatternNameLocation(PatternSyntax pattern, 
            SyntaxInfoTable syntaxInfoTable, IReadOnlyDictionary<Uri, Document> documents)
        {
            if (pattern.Name is null)
                throw new Exception("Pattern name location cannot be calculated for pattern without name");
            PatternSyntaxInfo info = syntaxInfoTable.GetSyntaxInfo(pattern);
            Document document = documents[info.Location.Uri];
            TextRange trimmedPatternRange = TrimStartKeywords(document.Text, pattern.TextRange);
            int startIndex = trimmedPatternRange.Start;
            if (document.Text[startIndex] is '#')
            {
                TextRange rangeWithoutHashSign = new(startIndex + 1, trimmedPatternRange.End);
                trimmedPatternRange = TrimStartTrivia(document.Text, rangeWithoutHashSign);
            }
            TextRange nameRange = GetMultipartIdentifierRange(document.Text, trimmedPatternRange);
            return GetTextRangeLocation(info.Location.Uri, nameRange, documents);
        }

        internal static Location GetPatternReferenceNameLocation(PatternReferenceSyntax reference,
            SyntaxInfoTable syntaxInfoTable, IReadOnlyDictionary<Uri, Document> documents)
        {
            ISyntaxInfo<PatternReferenceSyntax> info = syntaxInfoTable.GetSyntaxInfo(reference);
            Document document = documents[info.Location.Uri];
            TextRange nameRange = GetMultipartIdentifierRange(document.Text, reference.TextRange);
            return GetTextRangeLocation(info.Location.Uri, nameRange, documents);
        }

        internal static Location GetTextRangeLocation(Uri uri, TextRange range, IReadOnlyDictionary<Uri, Document> documents)
        {
            Document document = documents[uri];
            Position start = document.PositionAt(range.Start);
            Position end = document.PositionAt(range.End);
            return new Location(uri, new Range(start, end));
        }

        internal static Location GetExtractionLocation(Syntax syntax, 
            SyntaxInfoTable syntaxInfoTable, IReadOnlyDictionary<Uri, Document> documents)
        {
            ISyntaxInfo<Syntax> info = syntaxInfoTable.GetSyntaxInfo(syntax);
            Uri uri = info.Location.Uri;
            Document document = documents[uri];
            string fieldName = syntax switch
            {
                ExtractionSyntax extraction => extraction.FieldName,
                ExtractionFromFieldSyntax extractionFromField => extractionFromField.FieldName,
                _ => throw new Exception($"Unexpected syntax: {syntax}")
            };
            int start = syntax.TextRange.Start;
            int end = start + fieldName.Length;
            Range range = new(document.PositionAt(start), document.PositionAt(end));
            return new Location(uri, range);
        }

        internal static Location FetFieldReferenceLocation(FieldReferenceSyntax syntax, 
            SyntaxInfoTable syntaxInfoTable, IReadOnlyDictionary<Uri, Document> documents)
        {
            FieldReferenceSyntaxInfo info = syntaxInfoTable.GetSyntaxInfo(syntax);
            return TrimLocation(info.Location, documents);
        }

        internal static Location GetFromFieldLocation(ExtractionFromFieldSyntax extraction, 
            SyntaxInfoTable syntaxInfoTable, IReadOnlyDictionary<Uri, Document> documents)
        {
            if (extraction.FromFieldName is null)
                throw new Exception("Location cannot be calculated for ExtractionFromFieldSyntax with no FromFieldName");
            ISyntaxInfo<ExtractionFromFieldSyntax> info = syntaxInfoTable.GetSyntaxInfo(extraction);
            Document document = documents[info.Location.Uri];
            TextRange range = new(extraction.TextRange.Start + extraction.FieldName.Length, extraction.TextRange.End);
            range = TrimStartTrivia(document.Text, range);
            range = new TextRange(range.Start + 1, range.End); // Skip colon
            range = TrimStartTrivia(document.Text, range);
            Position fromFieldNameStart = document.PositionAt(range.Start);
            Position fromFieldNameEnd = document.PositionAt(range.Start + extraction.FromFieldName.Length);
            return new Location(info.Location.Uri, new Range(fromFieldNameStart, fromFieldNameEnd));
        }

        internal static Location GetLexemeLocation(Lexeme lexeme, Document document)
        {
            TextRange trimmedRange = TrimEndTrivia(document.Text, lexeme.TextRange);
            Position lexemeStart = document.PositionAt(trimmedRange.Start);
            Position lexemeEnd = document.PositionAt(trimmedRange.End);
            return new Location(document.Uri, new Range(lexemeStart, lexemeEnd));
        }

        internal static Location GetStringLiteralLocationWithoutQuotes(Lexeme lexeme, Document document)
        {
            if (lexeme.TokenId is not TokenId.StringLiteral)
                throw new ArgumentException(null, nameof(lexeme));
            string text = document.Text;
            TextRange trimmedRange = TrimEndTrivia(text, lexeme.TextRange);
            ReadOnlySpan<char> span = text.AsSpan(trimmedRange.Start, trimmedRange.Length);
            int stringWithoutQuotesLength = span.TrimStart("'\"").TrimEnd("*!").TrimEnd("'\"").Length;
            int startOffset = lexeme.TextRange.Start + 1;
            int endOffset = startOffset + stringWithoutQuotesLength;
            Position lexemeStart = document.PositionAt(startOffset);
            Position lexemeEnd = document.PositionAt(endOffset);
            return new Location(document.Uri, new Range(lexemeStart, lexemeEnd));
        }

        internal static Lexeme? GetPointedLexeme(PointerLocation location, out Syntax? parent,
            SyntaxInfoTable syntaxInfoTable, IReadOnlyDictionary<Uri, Document> documents,
            bool checkForPreviousLexeme = true, bool returnPreviousIfInEndOfFile = false)
        {
            SyntaxInfo<LinkedPackageSyntax> packageSyntax = syntaxInfoTable.GetLinkedPackageSyntaxInfo(location.Uri);
            Document document = documents[location.Uri];
            int offset = document.OffsetAt(location.Position);
            if (returnPreviousIfInEndOfFile && offset == document.Text.Length)
            {
                offset -= 1;
                // As cursor is shifted one character to the left, no check for previous lexeme should be done to avoid
                // returning incorrect (previous) lexeme.
                checkForPreviousLexeme = false;
            }                
            SyntaxResolver syntaxResolver = new();
            parent = syntaxResolver.Resolve(packageSyntax.Syntax, offset);
            Lexeme? result = null;
            if (parent is not null)
            {
                parent.CreateChildren(document.Text);
                Syntax? childSyntax = syntaxResolver.ResolveFromChildren(parent.Children, offset);
                result = childSyntax as Lexeme;
                if (checkForPreviousLexeme && result is { TokenId: not TokenId.Identifier } && result.TextRange.Start == offset)
                {
                    var previousSyntaxLocation = new PointerLocation(location.Uri, document.PositionAt(offset - 1));
                    result = GetPointedLexeme(previousSyntaxLocation, out parent, syntaxInfoTable, documents, checkForPreviousLexeme: false);
                }
            }
            return result;
        }

        internal static LinkedPackageSyntax[] GetAllLinkedPackages(SyntaxInfoTable syntaxInfoTable, 
            IReadOnlyDictionary<Uri, Document> documents) =>
            (from d in documents.Values
                let info = syntaxInfoTable.GetLinkedPackageSyntaxInfo(d.Uri)
                select info.Syntax).ToArray();

        internal static ISyntaxInfo<FieldSyntax>? GetFieldDefinition(PatternSyntax pattern, string fieldName, 
            SyntaxInfoTable syntaxInfoTable)
        {
            FieldSyntax? field = pattern.Fields.FirstOrDefault(f => f.Name == fieldName);
            return field is not null ? syntaxInfoTable.GetSyntaxInfo(field) : null;
        }

        private static void ValidateTextRange(string text, TextRange range)
        {
            if (range.Start < 0 || range.End > text.Length)
                throw new ArgumentException("Invalid text range");
        }
    }
}
