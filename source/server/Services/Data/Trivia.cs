using System.Collections.Generic;

namespace Nezaboodka.Nevod.Services
{
    internal readonly struct Trivia
    {
        public TriviaKind Kind { get; }
        public TextRange TextRange { get; }

        public Trivia(TriviaKind kind, TextRange textRange)
        {
            Kind = kind;
            TextRange = textRange;
        }
    }

    internal enum TriviaKind
    {
        Whitespaces,
        NewLine,
        SingleLineComment,
        MultiLineComment
    }

    internal readonly struct LexemeTriviaInfo
    {
        public TextRange TrimmedRange { get; }
        public List<Trivia> Trivia { get; }

        public LexemeTriviaInfo(TextRange trimmedRange, List<Trivia> trivia)
        {
            TrimmedRange = trimmedRange;
            Trivia = trivia;
        }
    }
}
