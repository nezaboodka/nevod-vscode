using System;
using System.Collections.Generic;
using System.Linq;

namespace Nezaboodka.Nevod.Services
{
    internal class FormattingVisitor
    {
        private const string DefaultNewLine = "\r\n";

        private static readonly HashSet<TokenId> s_binaryOperators = new()
        {
            TokenId.Plus, TokenId.Underscore, TokenId.Ellipsis, TokenId.DoublePeriod, TokenId.Equal,
            TokenId.Amphersand, TokenId.InsideKeyword, TokenId.OutsideKeyword, TokenId.HavingKeyword
        };

        private static readonly HashSet<TokenId> s_unaryOperators = new()
            { TokenId.Question, TokenId.Tilde, TokenId.HashSign };

        private static readonly HashSet<TokenId> s_openBraces = new()
            { TokenId.OpenParenthesis, TokenId.OpenCurlyBrace, TokenId.OpenSquareBracket };

        private static readonly HashSet<TokenId> s_closeBraces = new()
            { TokenId.CloseParenthesis, TokenId.CloseCurlyBrace, TokenId.CloseSquareBracket };

        private static readonly HashSet<TokenId> s_attributeTokens = new()
            { TokenId.Identifier, TokenId.IntegerLiteral, TokenId.Plus, TokenId.Minus };

        private static readonly HashSet<TokenId> s_numericRangeTokens = new()
            { TokenId.IntegerLiteral, TokenId.Plus, TokenId.Minus };

        private static readonly HashSet<TokenId> s_multipartIdentifierTokensWithoutPeriod = new()
            { TokenId.Identifier, TokenId.Asterisk };

        private static readonly HashSet<TokenId> s_periodToken = new() { TokenId.Period };

        private static readonly HashSet<TokenId> s_nonOperatorKeywordTokens = new()
        {
            TokenId.PatternKeyword, TokenId.SearchKeyword,
            TokenId.RequireKeyword, TokenId.NamespaceKeyword, TokenId.WhereKeyword
        };

        private static readonly FormattingConfiguration s_defaultConfiguration = new()
        {
            PlaceOpenBraceOnNewLine = false
        };

        private Document _document = null!; // Initialized in CreateFormattingEdits
        private List<TextEdit> _textEdits = null!; // Initialized in CreateFormattingEdits
        private FormattingOptions _formattingOptions = null!; // Initialized in CreateFormattingEdits
        private string _newLine = null!; // Initialized in CreateFormattingEdits
        private TextRange _formattingRange;
        private uint _currentIndentation;
        private bool _isLastTriviaNewLine;
        private Syntax? _contextSyntax; // Common syntax for previous and current lexemes
        private Lexeme? _previousLexeme;
        private Syntax? _previousParent;
        private LexemeTriviaInfo _previousLexemeTriviaInfo;
        private FormattingConfiguration? _configuration;
        private List<FormattingRule>? _rules;

        private List<FormattingRule> Rules
        {
            get
            {
                if (_rules is null)
                {
                    FormattingConfiguration configuration = _configuration ?? s_defaultConfiguration;
                    _rules = CreateFormattingRules(configuration);
                }
                return _rules;
            }
        }

        internal void UpdateConfiguration(FormattingConfiguration? configuration)
        {
            _configuration = configuration;
            _rules = null;
        }

        internal TextEdit[] CreateFormattingEdits(PackageSyntax package, Document document,
            FormattingOptions formattingOptions)
        {
            TextRange range = new(0, document.Text.Length);
            return CreateFormattingEdits(package, document, range, formattingOptions);
        }

        internal TextEdit[] CreateFormattingEdits(PackageSyntax package, Document document, TextRange range,
            FormattingOptions formattingOptions)
        {
            _document = document;
            _formattingRange = range;
            _formattingOptions = formattingOptions;
            _textEdits = new List<TextEdit>();
            _currentIndentation = 0;
            _contextSyntax = null;
            _previousLexeme = null;
            _previousParent = null;
            _newLine = formattingOptions.NewLine ?? DefaultNewLine;
            _previousLexemeTriviaInfo = Utils.GetLexemeTriviaInfo(_document.Text, new TextRange(0, package.TextRange.Start));
            _isLastTriviaNewLine = IsWhitespaceOrNewlineOnlyTrivia(_previousLexemeTriviaInfo.Trivia) ||
                                   IsLastTriviaNewLine(_previousLexemeTriviaInfo.Trivia);
            VisitSyntax(package, parent: null, parentIndentation: 0);
            TextEdit[] result = _textEdits.ToArray();
            _document = null!;
            _textEdits = null!;
            _formattingOptions = null!;
            _newLine = null!;
            return result;
        }

        private void VisitSyntax(Syntax? node, Syntax? parent, uint parentIndentation)
        {
            if (node != null)
            {
                node = ReplaceSystemPattern(node);
                if (FormattingRangeOverlapsWith(node.TextRange))
                {
                    int nodeStartLine = _document.PositionAt(node.TextRange.Start).Line;
                    node.CreateChildren(_document.Text);
                    foreach (Syntax child in node.Children)
                    {
                        if (child is Lexeme lexeme)
                            VisitLexeme(lexeme, node, parent, parentIndentation);
                        else
                        {
                            uint saveIndentation = _currentIndentation;
                            int childSyntaxStartLine = _document.PositionAt(child.TextRange.Start).Line;
                            if (nodeStartLine != childSyntaxStartLine && ShouldIdentChildren(node))
                                _currentIndentation = saveIndentation + _formattingOptions.TabSize;
                            VisitSyntax(child, parent: node, parentIndentation: saveIndentation);
                            _currentIndentation = saveIndentation;
                        }
                        _contextSyntax = node;
                    }
                }
                else
                    // Visit rightmost lexeme to save information about previous lexeme and trivia for case when
                    // next lexeme is inside formatting range.
                    VisitRightmostLexeme(node);
            }
        }

        private void VisitRightmostLexeme(Syntax syntax)
        {
            var isRightmostLexemeVisited = false;
            syntax.CreateChildren(_document.Text);
            while (!isRightmostLexemeVisited && syntax.Children.Count != 0)
            {
                Syntax rightmostChild = syntax.Children[^1];
                if (rightmostChild is Lexeme lexeme)
                {
                    // Visiting rightmost lexeme will not affect indentation, so pass null for grandparent and 0 for
                    // grandparent indentation.
                    VisitLexeme(lexeme, syntax, grandparent: null, grandparentIndentation: 0);
                    isRightmostLexemeVisited = true;
                }
                else
                {
                    syntax = rightmostChild;
                    syntax.CreateChildren(_document.Text);
                }
            }
        }

        private void VisitLexeme(Lexeme lexeme, Syntax parent, Syntax? grandparent, uint grandparentIndentation)
        {
            LexemeTriviaInfo triviaInfo = Utils.GetLexemeTriviaInfo(_document.Text, lexeme.TextRange);
            if (FormattingRangeOverlapsWith(triviaInfo.TrimmedRange))
            {
                NewLineAction newLineAction = ApplySuitableRule(lexeme, parent);
                // Recalculate indentation if new line was added or removed.
                // For example, in the following case P1 = Word @where { P2 = Num; }; indentation for nested pattern will be recalculated.
                // Recalculate only if lexeme is at the beginning of its parent.
                switch (newLineAction)
                {
                    case NewLineAction.Add:
                        if (lexeme.TextRange.Start == parent.TextRange.Start && grandparent is not null &&
                            ShouldIdentChildren(grandparent))
                            _currentIndentation = grandparentIndentation + _formattingOptions.TabSize;
                        break;
                    case NewLineAction.Delete:
                        if (lexeme.TextRange.Start == parent.TextRange.Start && grandparent is not null &&
                            ShouldIdentChildren(grandparent))
                            _currentIndentation = grandparentIndentation;
                        break;
                }
                if (_isLastTriviaNewLine && newLineAction is not NewLineAction.Delete ||
                    newLineAction is NewLineAction.Add)
                {
                    int parentStartLine = _document.PositionAt(parent.TextRange.Start).Line;
                    int lexemeStartLine = _document.PositionAt(lexeme.TextRange.Start).Line;
                    bool addDelta = lexemeStartLine != parentStartLine && ShouldAddDeltaForLexeme(lexeme, parent);
                    uint indentation = addDelta ? _currentIndentation + _formattingOptions.TabSize : _currentIndentation;
                    IdentSingleLineComments(_previousLexemeTriviaInfo.Trivia, indentation);
                    AddIndentationIfRequired(lexeme.TextRange, isLineAdded: newLineAction is NewLineAction.Add, indentation);
                }
            }
            _isLastTriviaNewLine = IsLastTriviaNewLine(triviaInfo.Trivia);
            _previousLexeme = lexeme;
            _previousParent = parent;
            _previousLexemeTriviaInfo = triviaInfo;
        }

        private void IdentSingleLineComments(List<Trivia> triviaList, uint indentation)
        {
            // Initial value should be set to true only if first trivia is in the beginning of the file.
            var shouldIndent = triviaList.Count != 0 && triviaList[0].TextRange.Start == 0;
            foreach (Trivia trivia in triviaList)
            {
                switch (trivia.Kind)
                {
                    case TriviaKind.NewLine:
                        shouldIndent = true;
                        break;
                    case TriviaKind.MultiLineComment:
                        shouldIndent = false;
                        break;
                    case TriviaKind.SingleLineComment:
                        if (shouldIndent && FormattingRangeOverlapsWith(trivia.TextRange))
                            AddIndentationIfRequired(trivia.TextRange, isLineAdded: false, indentation);
                        shouldIndent = false;
                        break;
                }
            }
        }

        private bool ShouldIdentChildren(Syntax syntax)
        {
            if (syntax is PackageSyntax)
                return false;
            return true;
        }

        private bool ShouldAddDeltaForLexeme(Lexeme lexeme, Syntax parent)
        {
            if (parent is PackageSyntax)
                return false;
            if (lexeme.TokenId is 
                TokenId.OpenCurlyBrace or
                TokenId.CloseCurlyBrace or
                TokenId.OpenSquareBracket or
                TokenId.CloseSquareBracket or
                TokenId.OpenParenthesis or
                TokenId.CloseParenthesis or
                TokenId.WhereKeyword)
                return false;
            return true;
        }

        private NewLineAction ApplySuitableRule(Lexeme lexeme, Syntax parent)
        {
            if (_previousLexeme is null || _previousParent is null || _contextSyntax is null)
                return NewLineAction.None;
            if (!IsWhitespaceOrNewlineOnlyTrivia(_previousLexemeTriviaInfo.Trivia))
                return NewLineAction.None;
            NewLineAction newLineAction = NewLineAction.None;
            FormattingContext context = new()
            {
                LeftTokenId = _previousLexeme.TokenId,
                LeftParentType = _previousParent.GetType(),
                RightTokenId = lexeme.TokenId,
                RightParentType = parent.GetType(),
                ContextSyntaxType = _contextSyntax.GetType()
            };
            FormattingRule? rule = Rules.FirstOrDefault(rule => rule.ShouldBeApplied(context));
            if (rule is not null)
            {
                bool areLexemesOnDifferentLines = _previousLexemeTriviaInfo.Trivia.Any(t => t.Kind is TriviaKind.NewLine);
                int rangeStart = _previousLexemeTriviaInfo.TrimmedRange.End;
                int rangeEnd = lexeme.TextRange.Start;
                switch (rule.FormattingAction)
                {
                    case FormattingAction.AddWhitespace:
                        if (areLexemesOnDifferentLines && !rule.CanDeleteNewLine)
                            break;
                        if (rangeEnd - rangeStart != 1 || _document.Text[rangeStart] != ' ')
                        {
                            AddTextEdit(rangeStart, rangeEnd - rangeStart, " ");
                            if (areLexemesOnDifferentLines)
                                newLineAction = NewLineAction.Delete;
                        }
                        break;
                    case FormattingAction.DeleteWhitespace:
                        if (areLexemesOnDifferentLines && !rule.CanDeleteNewLine)
                            break;
                        if (rangeStart != rangeEnd)
                        {
                            AddTextEdit(rangeStart, rangeEnd - rangeStart, "");
                            if (areLexemesOnDifferentLines)
                                newLineAction = NewLineAction.Delete;
                        }
                        break;
                    case FormattingAction.AddNewLine:
                        if (_previousLexemeTriviaInfo.Trivia.Any(t => t.Kind is TriviaKind.NewLine))
                            break;
                        int newLineRangeStart = lexeme.TextRange.Start;
                        if (_previousLexemeTriviaInfo.Trivia.Count != 0 &&
                            _previousLexemeTriviaInfo.Trivia[^1].Kind is TriviaKind.Whitespaces)
                            newLineRangeStart = _previousLexemeTriviaInfo.Trivia[^1].TextRange.Start;
                        AddTextEdit(newLineRangeStart, rangeEnd - newLineRangeStart, _newLine);
                        newLineAction = NewLineAction.Add;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(rule.FormattingAction),
                            "Unknown formatting action");
                }
            }
            return newLineAction;
        }

        private bool IsWhitespaceOrNewlineOnlyTrivia(List<Trivia> trivia) =>
            trivia.Count == 0 || trivia.All(t => t.Kind is TriviaKind.Whitespaces or TriviaKind.NewLine);

        private bool IsLastTriviaNewLine(List<Trivia> trivia) =>
            trivia.Count != 0 && trivia[^1].Kind is TriviaKind.NewLine ||
            trivia.Count > 1 && trivia[^1].Kind is TriviaKind.Whitespaces &&
            trivia[^2].Kind is TriviaKind.NewLine;

        private void AddIndentationIfRequired(TextRange rangeToIdent, bool isLineAdded, uint indentation)
        {
            if (isLineAdded)
            {
                if (indentation != 0)
                {
                    string indentationString = GetIndentationString(indentation);
                    AddTextEdit(rangeToIdent.Start, 0, indentationString);
                }
            }
            else
            {
                string indentationString = GetIndentationString(indentation);
                Position startPosition = _document.PositionAt(rangeToIdent.Start);
                int lineStart = _document.LineStartOffset(startPosition.Line);
                ReadOnlySpan<char> actualIndentation = _document.Text.AsSpan(lineStart, startPosition.Character);
                if (!actualIndentation.Equals(indentationString, StringComparison.Ordinal))
                    AddTextEdit(lineStart, startPosition.Character, indentationString);
            }
        }

        private string GetIndentationString(uint indentation)
        {
            if (_formattingOptions.InsertSpaces)
                return new string(' ', (int)indentation);
            else
                return new string('\t', (int)(indentation / _formattingOptions.TabSize));
        }

        private void AddTextEdit(int start, int length, string newTest)
        {
            Position startPosition = _document.PositionAt(start);
            Position endPosition = _document.PositionAt(start + length);
            Location location = new(_document.Uri, new Range(startPosition, endPosition));
            TextEdit textEdit = new(location, newTest);
            _textEdits.Add(textEdit);
        }

        private Syntax ReplaceSystemPattern(Syntax node)
        {
            // Any, Blank and WordBreak system patterns are unfolded into variation in parser, whose elements
            // have empty text range. Replace variation with TokenSyntax for correct formatting.
            if (node is VariationSyntax variation && variation.Elements.Count != 0 &&
                variation.Elements[0].TextRange.IsEmpty)
            {
                TokenSyntax syntax = Syntax.Token((TokenKind)(-1));
                syntax.TextRange = node.TextRange;
                return syntax;
            }
            return node;
        }

        private List<FormattingRule> CreateFormattingRules(FormattingConfiguration configuration)
        {
            static bool IsBinaryOperatorContext(FormattingContext context)
            {
                Type contextType = context.ContextSyntaxType;
                return contextType == typeof(SequenceSyntax) ||
                       contextType == typeof(WordSequenceSyntax) ||
                       contextType == typeof(AnySpanSyntax) ||
                       contextType == typeof(WordSpanSyntax) ||
                       contextType == typeof(ConjunctionSyntax) ||
                       contextType == typeof(InsideSyntax) ||
                       contextType == typeof(OutsideSyntax) ||
                       contextType == typeof(HavingSyntax) ||
                       contextType == typeof(PatternSyntax) /* Equal sign */;
            }
            static bool IsUnaryOperatorContext(FormattingContext context)
            {
                Type contextType = context.ContextSyntaxType;
                return contextType == typeof(OptionalitySyntax) ||
                       contextType == typeof(ExceptionSyntax) ||
                       contextType == typeof(FieldSyntax) /* Internal field */;
            }
            static bool IsBraceContext(FormattingContext context)
            {
                Type contextType = context.ContextSyntaxType;
                return contextType == typeof(VariationSyntax) ||
                       contextType == typeof(SpanSyntax) ||
                       contextType == typeof(PatternReferenceSyntax) ||
                       contextType == typeof(WordSpanSyntax) /* Span range */ ||
                       contextType == typeof(PatternSyntax) /* Field declaration */ ||
                       contextType == typeof(TokenSyntax) /* Attributes */ ||
                       contextType == typeof(TextSyntax) /* Attributes */;
            }
            static bool IsCommaContext(FormattingContext context)
            {
                Type contextType = context.ContextSyntaxType;
                return contextType == typeof(VariationSyntax) ||
                       contextType == typeof(SpanSyntax) ||
                       contextType == typeof(PatternReferenceSyntax) ||
                       contextType == typeof(PatternSyntax) /* Field declaration */ ||
                       contextType == typeof(TokenSyntax) /* Attributes */ ||
                       contextType == typeof(TextSyntax) /* Attributes */;
            }
            static bool IsFieldListOrAttributesContext(FormattingContext context)
            {
                Type contextType = context.ContextSyntaxType;
                return contextType == typeof(PatternSyntax) ||
                       contextType == typeof(PatternReferenceSyntax) ||
                       contextType == typeof(TokenSyntax) ||
                       contextType == typeof(TextSyntax);
            }
            static bool IsAttributesContext(FormattingContext context)
            {
                Type contextType = context.ContextSyntaxType;
                return contextType == typeof(TokenSyntax) ||
                       contextType == typeof(TextSyntax);
            }
            static bool IsNumericRangeContext(FormattingContext context)
            {
                Type contextType = context.ContextSyntaxType;
                return contextType == typeof(RepetitionSyntax) ||
                       contextType == typeof(WordSpanSyntax) /* Span range */ ||
                       contextType == typeof(TokenSyntax) ||
                       contextType == typeof(TextSyntax);
            }
            static bool IsAfterNumericRangeInRepetition(FormattingContext context)
            {
                return s_numericRangeTokens.Contains(context.LeftTokenId) &&
                       context.ContextSyntaxType == typeof(RepetitionSyntax);
            }
            static bool IsExtractionContext(FormattingContext context)
            {
                Type contextType = context.ContextSyntaxType;
                return contextType == typeof(ExtractionSyntax) ||
                       contextType == typeof(ExtractionFromFieldSyntax) ||
                       contextType == typeof(WordSpanSyntax) /* Span extraction */;
            }
            static bool IsMultipartIdentifierContext(FormattingContext context)
            {
                Type contextType = context.ContextSyntaxType;
                return contextType == typeof(PatternReferenceSyntax) ||
                       contextType == typeof(PatternSyntax);
            }
            static bool IsColonContext(FormattingContext context)
            {
                Type contextType = context.ContextSyntaxType;
                return contextType == typeof(PatternSyntax) ||
                       contextType == typeof(RequiredPackageSyntax);
            }
            var rules = new List<FormattingRule>
            {
                new("WhitespaceBetweenWhereKeywordAndOpenCurlyBrace", FormattingAction.AddWhitespace, true,
                    TokenId.WhereKeyword, typeof(PatternSyntax),
                    TokenId.OpenCurlyBrace, typeof(PatternSyntax),
                    _ => !configuration.PlaceOpenBraceOnNewLine),
                new("NewLineAfterWhereKeywordInPattern", FormattingAction.AddNewLine, false,
                    TokenId.WhereKeyword, typeof(PatternSyntax),
                    TokenId.OpenCurlyBrace, typeof(PatternSyntax),
                    _ => configuration.PlaceOpenBraceOnNewLine),
                new("NewLineAfterOpenCurlyBraceInPattern", FormattingAction.AddNewLine, false,
                    TokenId.OpenCurlyBrace, typeof(PatternSyntax),
                    FormattingRule.AnyTokenId, FormattingRule.AnyParentType),
                new("NewLineBeforeCloseCurlyBraceInPattern", FormattingAction.AddNewLine, false,
                    FormattingRule.AnyTokenId, FormattingRule.AnyParentType,
                    TokenId.CloseCurlyBrace, typeof(PatternSyntax)),
                new("WhitespaceBeforeWhereKeyword", FormattingAction.AddWhitespace, false,
                    FormattingRule.AnyTokenId, FormattingRule.AnyParentType,
                    TokenId.WhereKeyword, typeof(PatternSyntax)),
                new("WhitespaceBeforeBinaryOperator", FormattingAction.AddWhitespace, false,
                    FormattingRule.AnyTokenIds, s_binaryOperators, IsBinaryOperatorContext),
                new("WhitespaceAfterBinaryOperator", FormattingAction.AddWhitespace, false,
                    s_binaryOperators, FormattingRule.AnyTokenIds, IsBinaryOperatorContext),
                new("WhitespaceAfterKeyword", FormattingAction.AddWhitespace, false,
                    s_nonOperatorKeywordTokens, FormattingRule.AnyTokenIds),
                new("NoWhitespaceAfterUnaryOperator", FormattingAction.DeleteWhitespace, false,
                    s_unaryOperators, FormattingRule.AnyTokenIds, IsUnaryOperatorContext),
                new("NoWhitespaceAfterOpenBrace", FormattingAction.DeleteWhitespace, false,
                    s_openBraces, FormattingRule.AnyTokenIds, IsBraceContext),
                new("NoWhitespaceBeforeCloseBrace", FormattingAction.DeleteWhitespace, false,
                    FormattingRule.AnyTokenIds, s_closeBraces, IsBraceContext),
                new("WhitespaceAfterComma", FormattingAction.AddWhitespace, false,
                    TokenId.Comma, FormattingRule.AnyTokenId, IsCommaContext),
                new("NoWhitespaceBeforeComma", FormattingAction.DeleteWhitespace, false,
                    FormattingRule.AnyTokenId, TokenId.Comma, IsCommaContext),
                new("NoWhitespaceBeforeOpenParenthesisInFieldsOrAttributes", FormattingAction.DeleteWhitespace, false,
                    FormattingRule.AnyTokenId, TokenId.OpenParenthesis, IsFieldListOrAttributesContext),
                // NoWhitespaceInNumericRange should have higher priority than WhitespaceBetweenAttributes not to add
                // whitespaces between numeric range parts in attributes.
                new("NoWhitespaceInNumericRange", FormattingAction.DeleteWhitespace, false,
                    s_numericRangeTokens, s_numericRangeTokens, IsNumericRangeContext),
                new("WhitespaceBetweenAttributes", FormattingAction.AddWhitespace, false,
                    s_attributeTokens, s_attributeTokens, IsAttributesContext),
                new("WhitespaceAfterNumericRangeInRepetition", FormattingAction.AddWhitespace, false,
                    s_numericRangeTokens, FormattingRule.AnyTokenIds, IsAfterNumericRangeInRepetition),
                new("NoWhitespaceBeforeColonInExtraction", FormattingAction.DeleteWhitespace, false,
                    FormattingRule.AnyTokenId, TokenId.Colon, IsExtractionContext),
                new("WhitespaceAfterColonInExtraction", FormattingAction.AddWhitespace, false,
                    TokenId.Colon, FormattingRule.AnyTokenId, IsExtractionContext),
                new("NoWhitespaceBeforePeriodInMultipartIdentifier", FormattingAction.DeleteWhitespace, false,
                    s_multipartIdentifierTokensWithoutPeriod, s_periodToken, IsMultipartIdentifierContext),
                new("NoWhitespaceAfterPeriodInMultipartIdentifier", FormattingAction.DeleteWhitespace, false,
                    s_periodToken, s_multipartIdentifierTokensWithoutPeriod, IsMultipartIdentifierContext),
                new("WhitespaceAfterWordSpanRange", FormattingAction.AddWhitespace, false,
                    TokenId.CloseSquareBracket, typeof(WordSpanSyntax),
                    FormattingRule.AnyTokenId, FormattingRule.AnyParentType),
                new("NoWhitespaceBeforeSemicolon", FormattingAction.DeleteWhitespace, true,
                    FormattingRule.AnyTokenId, FormattingRule.AnyParentType,
                    TokenId.Semicolon, FormattingRule.AnyParentType, IsColonContext),
                new("NoWhitespaceAfterHashSignInPatternName", FormattingAction.DeleteWhitespace, true,
                    TokenId.HashSign, typeof(PatternSyntax),
                    TokenId.Identifier, typeof(PatternSyntax)),
                // Parenthesized expression does not have it's own syntax, so handle open and close parenthesis
                // separately from other braces with lower priority.
                new("NoWhitespaceAfterOpenParenthesis", FormattingAction.DeleteWhitespace, false,
                    TokenId.OpenParenthesis, FormattingRule.AnyTokenId),
                new("NoWhitespaceBeforeCloseParenthesis", FormattingAction.DeleteWhitespace, false,
                    FormattingRule.AnyTokenId, TokenId.CloseParenthesis)
            };
            return rules;
        }

        private bool FormattingRangeOverlapsWith(TextRange range)
        {
            int start = Math.Max(_formattingRange.Start, range.Start);
            int end = Math.Min(_formattingRange.End, range.End);
            return start < end;
        }

        private enum NewLineAction
        {
            None,
            Delete,
            Add
        }

        private enum FormattingAction
        {
            AddWhitespace,
            DeleteWhitespace,
            AddNewLine
        }

        private class FormattingRule
        {
            internal static readonly TokenId? AnyTokenId = null;
            internal static readonly HashSet<TokenId>? AnyTokenIds = null;
            internal static readonly Type? AnyParentType = null;

            // For debug purpose
            internal string Name { get; }
            internal FormattingAction FormattingAction { get; }
            internal bool CanDeleteNewLine { get; }

            private readonly TokenId? _leftTokenId;
            private readonly HashSet<TokenId>? _leftTokenIds;
            private readonly Type? _leftParentType;
            private readonly TokenId? _rightTokenId;
            private readonly HashSet<TokenId>? _rightTokenIds;
            private readonly Type? _rightParentType;
            private readonly Func<FormattingContext, bool>[] _contextChecks;

            internal FormattingRule(string name, FormattingAction formattingAction, bool canDeleteNewLine,
                TokenId? leftTokenId, TokenId? rightTokenId, params Func<FormattingContext, bool>[] contextChecks)
                : this(name, formattingAction, canDeleteNewLine, leftTokenId, leftTokenIds: null, leftParentType: null,
                    rightTokenId, rightTokenIds: null, rightParentType: null, contextChecks)
            {
            }

            internal FormattingRule(string name, FormattingAction formattingAction, bool canDeleteNewLine,
                TokenId? leftTokenId, Type? leftParentType, TokenId? rightTokenId, Type? rightParentType,
                params Func<FormattingContext, bool>[] contextChecks)
                : this(name, formattingAction, canDeleteNewLine, leftTokenId, leftTokenIds: null, leftParentType,
                    rightTokenId, rightTokenIds: null, rightParentType, contextChecks)
            {
            }

            internal FormattingRule(string name, FormattingAction formattingAction, bool canDeleteNewLine,
                HashSet<TokenId>? leftTokenIds, HashSet<TokenId>? rightTokenIds,
                params Func<FormattingContext, bool>[] contextChecks)
                : this(name, formattingAction, canDeleteNewLine, leftTokenId: null, leftTokenIds, leftParentType: null,
                    rightTokenId: null, rightTokenIds, rightParentType: null, contextChecks)
            {
            }

            internal FormattingRule(string name, FormattingAction formattingAction, bool canDeleteNewLine,
                TokenId? leftTokenId, HashSet<TokenId>? leftTokenIds, Type? leftParentType,
                TokenId? rightTokenId, HashSet<TokenId>? rightTokenIds, Type? rightParentType,
                params Func<FormattingContext, bool>[] contextChecks)
            {
                Name = name;
                FormattingAction = formattingAction;
                CanDeleteNewLine = canDeleteNewLine;
                _leftTokenId = leftTokenId;
                _leftTokenIds = leftTokenIds;
                _leftParentType = leftParentType;
                _rightTokenId = rightTokenId;
                _rightTokenIds = rightTokenIds;
                _rightParentType = rightParentType;
                _contextChecks = contextChecks;
            }

            internal bool ShouldBeApplied(FormattingContext context) =>
                // If token id set is not null, check if it contains token id, otherwise compare token id with _leftTokenId or _rightTokenIds 
                (_leftTokenIds is not null && _leftTokenIds.Contains(context.LeftTokenId) ||
                 _leftTokenIds is null && (_leftTokenId is null || _leftTokenId == context.LeftTokenId)) &&
                (_leftParentType is null || _leftParentType == context.LeftParentType) &&
                (_rightTokenIds is not null && _rightTokenIds.Contains(context.RightTokenId) ||
                 _rightTokenIds is null && (_rightTokenId is null || _rightTokenId == context.RightTokenId)) &&
                (_rightParentType is null || _rightParentType == context.RightParentType) &&
                (_contextChecks.Length == 0 || _contextChecks.All(check => check(context)));
        }

        private readonly struct FormattingContext
        {
            internal TokenId LeftTokenId { get; init; }
            internal Type LeftParentType { get; init; }
            internal TokenId RightTokenId { get; init; }
            internal Type RightParentType { get; init; }
            internal Type ContextSyntaxType { get; init; }

            internal FormattingContext(TokenId leftTokenId, Type leftParentType, TokenId rightTokenId,
                Type rightParentType,
                Type contextSyntaxType)
            {
                LeftTokenId = leftTokenId;
                LeftParentType = leftParentType;
                RightTokenId = rightTokenId;
                RightParentType = rightParentType;
                ContextSyntaxType = contextSyntaxType;
            }
        }
    }
}
