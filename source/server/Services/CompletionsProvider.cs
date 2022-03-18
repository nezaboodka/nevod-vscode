using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;

namespace Nezaboodka.Nevod.Services
{
    internal class CompletionsProvider
    {
        private static readonly Completion[] s_englishKeywordCompletions =
        {
            new(CompletionKind.Keyword, "@require"),
            new(CompletionKind.Keyword, "@namespace"),
            new(CompletionKind.Keyword, "@pattern"),
            new(CompletionKind.Keyword, "@search"),
            new(CompletionKind.Keyword, "@inside"),
            new(CompletionKind.Keyword, "@outside"),
            new(CompletionKind.Keyword, "@having"),
            new(CompletionKind.Keyword, "@where")
        };

        private static readonly Completion[] s_russianKeywordCompletions =
        {
            new(CompletionKind.Keyword, "@требуется"),
            new(CompletionKind.Keyword, "@пространство"),
            new(CompletionKind.Keyword, "@шаблон"),
            new(CompletionKind.Keyword, "@искать"),
            new(CompletionKind.Keyword, "@где"),
            new(CompletionKind.Keyword, "@внутри"),
            new(CompletionKind.Keyword, "@вне"),
            new(CompletionKind.Keyword, "@имеющий")
        };

        private static readonly Completion[] s_wordAttributeCompletions =
        {
            new(CompletionKind.TextAttribute, "Lowercase"),
            new(CompletionKind.TextAttribute, "Uppercase"),
            new(CompletionKind.TextAttribute, "TitleCase")
        };

        private static readonly Completion[] s_textAttributeCompletions =
        {
            new(CompletionKind.TextAttribute, "Alpha"),
            new(CompletionKind.TextAttribute, "Num"),
            new(CompletionKind.TextAttribute, "AlphaNum"),
            new(CompletionKind.TextAttribute, "NumAlpha"),
            new(CompletionKind.TextAttribute, "Lowercase"),
            new(CompletionKind.TextAttribute, "Uppercase"),
            new(CompletionKind.TextAttribute, "TitleCase")
        };

        private static readonly Completion[] s_tokenCompletions =
        {
            new(CompletionKind.Token, "Word"),
            new(CompletionKind.Token, "Punct"),
            new(CompletionKind.Token, "Symbol"),
            new(CompletionKind.Token, "Space"),
            new(CompletionKind.Token, "LineBreak"),
            new(CompletionKind.Token, "Start"),
            new(CompletionKind.Token, "End"),
            new(CompletionKind.Token, "Alpha"),
            new(CompletionKind.Token, "Num"),
            new(CompletionKind.Token, "AlphaNum"),
            new(CompletionKind.Token, "NumAlpha"),
            new(CompletionKind.Token, "Blank"),
            new(CompletionKind.Token, "WordBreak"),
            new(CompletionKind.Token, "Any")
        };
        
        private static readonly Func<string?, string?> s_normalizePathCaseFunc;

        static CompletionsProvider()
        {
            string baseDirectory = AppContext.BaseDirectory;
            // If file system is not case sensitive, File.Exists for base directory path converted to upper case will return true.
            if (Directory.Exists(baseDirectory.ToUpper()) && Directory.Exists(baseDirectory.ToLower()))
                s_normalizePathCaseFunc = path => path?.ToLower();
            else
                s_normalizePathCaseFunc = path => path;
        }

        [return: NotNullIfNotNull("path")]
        private static string? NormalizePathCase(string? path) => s_normalizePathCaseFunc(path);

        private readonly ParentSyntaxFinder _parentSyntaxFinder = new();
        private readonly CompletionsFinder _completionsFinder = new();
        private SyntaxInfoTable _syntaxInfoTable = null!; // Initialized in GetCompletions
        private IReadOnlyDictionary<Uri, Document> _documents = null!; // Initialized in GetCompletions

        internal IEnumerable<Completion>? GetCompletions(PointerLocation location, 
            SyntaxInfoTable syntaxInfoTable, IReadOnlyDictionary<Uri, Document> documents)
        {
            _syntaxInfoTable = syntaxInfoTable;
            _documents = documents;
            Lexeme? lexeme = Utils.GetPointedLexeme(location, out Syntax? parent, 
                _syntaxInfoTable, _documents, checkForPreviousLexeme: true, returnPreviousIfInEndOfFile: true);
            if (IsLocationInComment(location, lexeme))
            {
                _syntaxInfoTable = null!;
                _documents = null!;
                return null;
            }
            Completion[]? completions = null;
            if (lexeme is not null)
            {
                switch (parent)
                {
                    case ExtractionSyntax extraction:
                        completions = GetCompletionsForExtraction(location, extraction, lexeme);
                        completions = AddKeywordCompletions(completions);
                        break;
                    case FieldReferenceSyntax fieldReference:
                        completions = GetCompletionsForFieldReference(location, fieldReference, lexeme);
                        completions = AddKeywordCompletions(completions);
                        break;
                    case ExtractionFromFieldSyntax extractionFromField:
                        completions = GetCompletionsForExtractionFromField(location, extractionFromField, lexeme);
                        completions = AddKeywordCompletions(completions);
                        break;
                    case PatternReferenceSyntax patternReference:
                        completions = GetCompletionsForPatternReference(location, patternReference, lexeme);
                        completions = AddKeywordCompletions(completions);
                        break;
                    case SearchTargetSyntax searchTarget:
                        completions = GetCompletionsForSearchTarget(location, searchTarget, lexeme);
                        completions = AddKeywordCompletions(completions);
                        break;
                    case TextSyntax text:
                        completions = GetCompletionsForText(location, text, lexeme, out bool isLocationInsideString);
                        // Do not show any completions if cursor is inside string
                        if (!isLocationInsideString)
                            completions = AddKeywordCompletions(completions);
                        break;
                    case TokenSyntax token:
                        completions = GetCompletionsForToken(location, token, lexeme);
                        completions = AddKeywordCompletions(completions);
                        break;
                    case RequiredPackageSyntax requiredPackage:
                        completions = GetCompletionsForRequiredPackage(location, requiredPackage, lexeme, 
                            out bool isLocationInsidePath);
                        // Do not show any completions if cursor is inside string
                        if (!isLocationInsidePath)
                            completions = AddKeywordCompletions(completions);
                        break;
                    default:
                        completions = GetIdentifierCompletionsIfInValidPlace(location.Uri, lexeme, parent);
                        completions = AddKeywordCompletions(completions);
                        break;
                }
            }
            else
                completions = AddKeywordCompletions(completions);
            _syntaxInfoTable = null!;
            _documents = null!;
            return completions;
        }

        private Completion[]? GetCompletionsForExtraction(PointerLocation location, ExtractionSyntax extraction, 
            Lexeme lexeme)
        {
            Completion[]? completions = null;
            if (lexeme.TokenId is TokenId.Identifier)
            {
                Document document = _documents[location.Uri];
                Location lexemeLocation = Utils.GetLexemeLocation(lexeme, document);
                if (lexemeLocation.Contains(location))
                {
                    ExtractionSyntaxInfo extractionFromFieldInfo = _syntaxInfoTable.GetSyntaxInfo(extraction);
                    completions = GetPatternFieldsCompletions(extractionFromFieldInfo.Pattern);
                }
            }
            else if (lexeme.TokenId is TokenId.Colon)
            {
                LinkedPackageSyntax package = _syntaxInfoTable.GetLinkedPackageSyntaxInfo(location.Uri).Syntax;
                PatternSyntax? parentPattern = _parentSyntaxFinder.FindParentPattern(package, lexeme);
                if (parentPattern is not null)
                {
                    completions = _completionsFinder.FindCompletions(_syntaxInfoTable.GetSyntaxInfo(parentPattern),
                        package, string.Empty);
                    completions = AddTokenCompletions(completions);
                }
            }
            return completions;
        }

        private Completion[]? GetCompletionsForFieldReference(PointerLocation location, 
            FieldReferenceSyntax fieldReference, Lexeme lexeme)
        {
            Completion[]? completions = null;
            if (lexeme.TokenId is TokenId.Identifier)
            {
                Document document = _documents[location.Uri];
                Location lexemeLocation = Utils.GetLexemeLocation(lexeme, document);
                if (lexemeLocation.Contains(location))
                {
                    FieldReferenceSyntaxInfo fieldReferenceInfo = _syntaxInfoTable.GetSyntaxInfo(fieldReference);
                    completions = GetPatternFieldsCompletions(fieldReferenceInfo.Pattern);
                }
            }
            return completions;
        }

        private Completion[]? GetCompletionsForExtractionFromField(PointerLocation location,
            ExtractionFromFieldSyntax extractionFromField, Lexeme lexeme)
        {
            Completion[]? completions = null;
            ExtractionFromFieldSyntaxInfo extractionFromFieldSyntaxInfo = _syntaxInfoTable.GetSyntaxInfo(extractionFromField);
            // Do not show fields completions if referenced pattern has no fields.
            PatternSyntax? referencedPattern = extractionFromFieldSyntaxInfo.PatternReference.ReferencedPattern;
            if (referencedPattern?.Fields.Count > 0)
                if (lexeme.TokenId is TokenId.Colon)
                    completions = GetPatternFieldsCompletions(referencedPattern);
                else if (lexeme.TokenId is TokenId.Identifier)
                {
                    Document document = _documents[location.Uri];
                    Location lexemeLocation = Utils.GetLexemeLocation(lexeme, document);
                    LinkedPackageSyntax package = _syntaxInfoTable.GetLinkedPackageSyntaxInfo(location.Uri).Syntax;
                    PatternSyntax? parentPattern = _parentSyntaxFinder.FindParentPattern(package, lexeme);
                    if (lexemeLocation.Contains(location) && parentPattern is not null)
                    {
                        if (ReferenceEquals(extractionFromField.Children[0], lexeme)) // Field identifier
                            completions = GetPatternFieldsCompletions(parentPattern);
                        else // From field identifier
                            completions = GetPatternFieldsCompletions(extractionFromFieldSyntaxInfo.PatternReference.ReferencedPattern);
                    }
                }
            return completions;
        }

        private Completion[]? GetCompletionsForPatternReference(PointerLocation location, 
            PatternReferenceSyntax patternReference, Lexeme lexeme)
        {
            Completion[]? completions = null;
            LinkedPackageSyntax package = _syntaxInfoTable.GetLinkedPackageSyntaxInfo(location.Uri).Syntax;
            Syntax? patternReferenceParent = _parentSyntaxFinder.FindParentSyntax(package, patternReference);
            if (patternReferenceParent is SearchTargetSyntax searchTarget)
            {
                if (lexeme.TokenId is TokenId.Identifier or TokenId.Period)
                {
                    Document document = _documents[location.Uri];
                    Location lexemeLocation = Utils.GetLexemeLocation(lexeme, document);
                    if (lexemeLocation.Contains(location))
                    {
                        string nameSpanBeforeCursor = GetIdentifierSpanBeforeCursor(patternReference.Children, location);
                        completions = _completionsFinder.FindSearchTargetCompletions(package, searchTarget.Namespace,
                            nameSpanBeforeCursor);
                    }
                }
            }
            else
            {
                PatternSyntax? parentPattern = _parentSyntaxFinder.FindParentPattern(package, lexeme);
                if (parentPattern is not null)
                    if (lexeme.TokenId is TokenId.OpenParenthesis or TokenId.Comma &&
                        patternReference.ReferencedPattern?.Fields.Count > 0)
                        completions = GetPatternFieldsCompletions(parentPattern);
                    else if (lexeme.TokenId is TokenId.Identifier or TokenId.Period)
                    {
                        Document document = _documents[location.Uri];
                        Location lexemeLocation = Utils.GetLexemeLocation(lexeme, document);
                        if (lexemeLocation.Contains(location))
                        {
                            string nameSpanBeforeCursor = GetIdentifierSpanBeforeCursor(patternReference.Children, location);
                            completions = _completionsFinder.FindCompletions(
                                _syntaxInfoTable.GetSyntaxInfo(parentPattern),
                                package,
                                nameSpanBeforeCursor);
                            completions = AddTokenCompletions(completions);
                        }
                    }
            }
            return completions;
        }

        private Completion[]? GetCompletionsForSearchTarget(PointerLocation location, SearchTargetSyntax searchTarget, Lexeme lexeme)
        {
            Completion[]? completions = null;
            if (lexeme.TokenId is TokenId.SearchKeyword)
            {
                Document document = _documents[location.Uri];
                Location lexemeLocation = Utils.GetLexemeLocation(lexeme, document);
                if (!lexemeLocation.Contains(location))
                {
                    LinkedPackageSyntax package = _syntaxInfoTable.GetLinkedPackageSyntaxInfo(location.Uri).Syntax;
                    completions = _completionsFinder.FindSearchTargetCompletions(package,
                        searchTarget.Namespace, inputValue: string.Empty);
                }
            }
            // Identifier and period lexemes are parts of NamespaceSearchTarget.
            // In PatterSearchTarget multipart identifier is parsed as PatternReferenceSyntax.
            else if (lexeme.TokenId is TokenId.Identifier or TokenId.Period)
            {
                Document document = _documents[location.Uri];
                Location lexemeLocation = Utils.GetLexemeLocation(lexeme, document);
                if (lexemeLocation.Contains(location))
                {
                    string nameSpanBeforeCursor = GetIdentifierSpanBeforeCursor(searchTarget.Children,
                        location, skipKeywords: true);
                    LinkedPackageSyntax package = _syntaxInfoTable.GetLinkedPackageSyntaxInfo(location.Uri).Syntax;
                    completions = _completionsFinder.FindSearchTargetCompletions(package,
                        searchTarget.Namespace, nameSpanBeforeCursor);
                }
            }
            return completions;
        }

        private Completion[]? GetCompletionsForText(PointerLocation location, TextSyntax text, Lexeme lexeme,
            out bool isLocationInsideString)
        {
            Completion[]? completions = null;
            isLocationInsideString = false;
            if (lexeme.TokenId is TokenId.StringLiteral or TokenId.UnterminatedStringLiteral)
            {
                Document document = _documents[location.Uri];
                Location stringLocation = Utils.GetLexemeLocation(lexeme, document);
                isLocationInsideString = stringLocation.ContainsExcludingEdges(location);
            }
            else if (lexeme.TokenId is not TokenId.CloseParenthesis)
            {
                int lexemeIndex = text.Children.IndexOf(lexeme);
                // Lexeme is after open parenthesis. Index 0 is string literal, index 1 - open parenthesis.
                if (lexemeIndex >= 1)
                    completions = s_textAttributeCompletions;
            }
            return completions;
        }

        private Completion[]? GetCompletionsForToken(PointerLocation location, TokenSyntax token, Lexeme lexeme)
        {
            Completion[]? completions = null;
            // Cursor is after identifier of token itself, not it's attributes.
            if (lexeme.TokenId is TokenId.Identifier && ReferenceEquals(token.Children[0], lexeme))
            {
                Document document = _documents[location.Uri];
                Location lexemeLocation = Utils.GetLexemeLocation(lexeme, document);
                if (lexemeLocation.Contains(location))
                    completions = s_tokenCompletions;
            }
            // Only Word tokens can have attributes.
            // Space and LineBreak can have only numeric ranges, no completions required.
            else if (lexeme.TokenId is not TokenId.CloseParenthesis && token is { TokenKind: TokenKind.Word })
            {
                int lexemeIndex = token.Children.IndexOf(lexeme);
                // Lexeme is after open parenthesis. Index 0 is identifier, index 1 - open parenthesis.
                if (lexemeIndex >= 1)
                    completions = s_wordAttributeCompletions;
            }
            return completions;
        }

        private Completion[]? GetCompletionsForRequiredPackage(PointerLocation location, 
            RequiredPackageSyntax requiredPackage, Lexeme lexeme, out bool isLocationInsidePath)
        {
            Completion[]? completions = null;
            isLocationInsidePath = false;
            if (lexeme.TokenId is TokenId.StringLiteral)
            {
                Document document = _documents[location.Uri];
                Location stringLocation = Utils.GetLexemeLocation(lexeme, document);
                isLocationInsidePath = stringLocation.ContainsExcludingEdges(location);
                Location textLocation = Utils.GetStringLiteralLocationWithoutQuotes(lexeme, document);
                Location textLocationWithTrimmedWhitespaces = Utils.TrimLocationWhitespaces(textLocation, _documents);
                if (textLocationWithTrimmedWhitespaces.Contains(location))
                {
                    string? baseDirectory = Path.GetDirectoryName(location.Uri.AbsolutePath);
                    string inputValue = requiredPackage.RelativePath;
                    string currentFile = location.Uri.AbsolutePath;
                    if (baseDirectory is not null)
                        completions = GetPathCompletions(baseDirectory, inputValue, currentFile, textLocation);
                }
            }
            return completions;
        }

        private Completion[]? GetIdentifierCompletionsIfInValidPlace(Uri documentUri, Lexeme lexeme, Syntax? parentSyntax)
        {
            Completion[]? completions = null;
            LinkedPackageSyntax package = _syntaxInfoTable.GetLinkedPackageSyntaxInfo(documentUri).Syntax;
            PatternSyntax? parentPattern = _parentSyntaxFinder.FindParentPattern(package, lexeme);
            if (parentPattern is not null && CanShowCompletionsAfter(lexeme, parentSyntax))
            {
                completions = _completionsFinder.FindCompletions(_syntaxInfoTable.GetSyntaxInfo(parentPattern),
                    package, string.Empty);
                completions = AddTokenCompletions(completions);
            }
            return completions;
        }

        private string GetIdentifierSpanBeforeCursor(IReadOnlyCollection<Syntax> children,
            PointerLocation cursorLocation, bool skipKeywords = false)
        {
            StringBuilder identifier = new();
            Document document = _documents[cursorLocation.Uri];
            int offset = document.OffsetAt(cursorLocation.Position);
            IEnumerable<Syntax> identifierParts = children;
            if (skipKeywords)
                identifierParts = children.SkipWhile(s => s is Lexeme
                {
                    TokenId: TokenId.SearchKeyword or TokenId.PatternKeyword
                });
            foreach (Syntax syntax in identifierParts)
            {
                TextRange range = syntax.TextRange;
                if (offset <= range.Start)
                    break;
                if (syntax is Lexeme { TokenId: TokenId.Identifier or TokenId.Period or TokenId.Asterisk } lexeme)
                {
                    if (offset < range.End)
                        range = new TextRange(range.Start, offset);
                    string identifierPart;
                    switch (lexeme.TokenId)
                    {
                        case TokenId.Identifier:
                            TextRange trimmedRange = Utils.TrimEndTrivia(document.Text, range);
                            identifierPart = document.Text.Substring(trimmedRange.Start, trimmedRange.Length);
                            break;
                        case TokenId.Period:
                            identifierPart = ".";
                            break;
                        case TokenId.Asterisk:
                            identifierPart = "*";
                            break;
                        default:
                            throw new ArgumentException();
                    }
                    identifier.Append(identifierPart);
                }
                else
                    break;
            }
            return identifier.ToString();
        }

        private Completion[] GetPathCompletions(string baseDirectory, string inputValue, string currentFile, Location textLocation)
        {
            string trimmedInputValue = inputValue.Trim(' ');
            bool PathHasNoFileName(string path) =>
                string.IsNullOrEmpty(trimmedInputValue) || path.EndsWith('/') || path.EndsWith('\\');
            string CombineNameWithInputValue(string name)
            {
                if (PathHasNoFileName(trimmedInputValue))
                    return trimmedInputValue + name;
                int lastIndexOfSlash = trimmedInputValue.LastIndexOfAny(new[] { '\\', '/' });
                if (lastIndexOfSlash != -1)
                {
                    int directoryPathLength = lastIndexOfSlash + 1;
                    return trimmedInputValue[..directoryPathLength] + name;
                }
                return name;
            }
            string path = Path.IsPathRooted(trimmedInputValue) ? trimmedInputValue : Path.Combine(baseDirectory, trimmedInputValue);
            string? inputBaseDirectory = PathHasNoFileName(path) ? path : Path.GetDirectoryName(path);
            if (inputBaseDirectory is not null && Directory.Exists(inputBaseDirectory))
            {
                try
                {
                    // Get full path for correct comparison with current file path
                    inputBaseDirectory = Path.GetFullPath(inputBaseDirectory);
                    string normalizedCurrentFile = NormalizePathCase(Path.GetFullPath(currentFile));
                    IEnumerable<Completion> fileCompletions = Directory.EnumerateFiles(inputBaseDirectory, "*.np")
                        .Where(p => NormalizePathCase(p) != normalizedCurrentFile)
                        .Select(Path.GetFileName)
                        .OfType<string>() // Remove null values
                        .Select(CombineNameWithInputValue)
                        .Select(insertPath => new Completion(CompletionKind.FilePath, Path.GetFileName(insertPath), 
                            new TextEdit(textLocation, insertPath), insertPath));
                    IEnumerable<Completion> directoryCompletions = Directory.EnumerateDirectories(inputBaseDirectory)
                        .Select(Path.GetFileName)
                        .OfType<string>() // Remove null values
                        .Select(CombineNameWithInputValue)
                        .Select(insertPath => new Completion(CompletionKind.DirectoryPath, Path.GetFileName(insertPath), 
                            new TextEdit(textLocation, insertPath), insertPath));
                    return fileCompletions.Concat(directoryCompletions).ToArray();
                }
                catch (Exception e) when (e is PathTooLongException or SecurityException or UnauthorizedAccessException)
                {
                    return Array.Empty<Completion>();
                }
            }
            else
                return Array.Empty<Completion>();
        }

        private Completion[] AddKeywordCompletions(Completion[]? completions)
        {
            Completion[] keywordCompletions = s_englishKeywordCompletions;
            return completions is null ? keywordCompletions : completions.Concat(keywordCompletions).ToArray();
        }

        private Completion[] AddTokenCompletions(Completion[]? completions) =>
            completions is null ? s_tokenCompletions : completions.Concat(s_tokenCompletions).ToArray();

        private bool IsLocationInComment(PointerLocation location, Lexeme? contextLexeme)
        {
            Document document = _documents[location.Uri];
            int offset = document.OffsetAt(location.Position);
            string text = document.Text;
            TextRange range;
            if (contextLexeme is not null)
                range = contextLexeme.TextRange;
            else
            {
                LinkedPackageSyntax packageSyntax = _syntaxInfoTable.GetLinkedPackageSyntaxInfo(location.Uri).Syntax;
                if (offset < packageSyntax.TextRange.Start)
                    range = new TextRange(0, packageSyntax.TextRange.Start);
                else
                    range = new TextRange(0, 0);
            }
            return Utils.IsOffsetInComment(offset, text, range);
        }

        private Completion[] GetPatternFieldsCompletions(PatternSyntax pattern) => 
            pattern.Fields.Select(field => new Completion(CompletionKind.Field, field.Name)).ToArray();

        private static bool CanShowCompletionsAfter(Lexeme lexeme, Syntax? parentSyntax)
        {
            return lexeme.TokenId is
                       TokenId.OpenSquareBracket or
                       TokenId.Plus or
                       TokenId.Amphersand or
                       TokenId.Question or
                       TokenId.Equal or
                       TokenId.Underscore or
                       TokenId.DoublePeriod or
                       TokenId.Ellipsis or
                       TokenId.InsideKeyword or
                       TokenId.OutsideKeyword or
                       TokenId.HavingKeyword ||
                   // Do not show completions after open parenthesis or comma in pattern fields declaration.
                   lexeme.TokenId is TokenId.OpenParenthesis or TokenId.Comma && parentSyntax is null or not PatternSyntax ||
                   // Do not show completions after open curly brace in nested patterns.
                   lexeme.TokenId is TokenId.OpenCurlyBrace && parentSyntax is null or not PatternSyntax;
        }
    }
}
