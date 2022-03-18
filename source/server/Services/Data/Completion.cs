using System;

namespace Nezaboodka.Nevod.Services
{
    public readonly struct Completion
    {
        public CompletionKind Kind { get; }
        public string Text { get; }
        public string? FilterText { get; }
        public string SortText { get; }
        public string? Context { get; }
        public string? InsertText { get; }
        public TextEdit? TextEdit { get; }

        public Completion(CompletionKind kind, string text)
        {
            Kind = kind;
            SortText = kind switch
            {
                CompletionKind.Field => CompletionPriority.Field,
                CompletionKind.Pattern => CompletionPriority.Pattern,
                CompletionKind.Namespace => CompletionPriority.Namespace,
                CompletionKind.Keyword => CompletionPriority.Keyword,
                CompletionKind.FilePath => CompletionPriority.FilePath,
                CompletionKind.DirectoryPath => CompletionPriority.DirectoryPath,
                CompletionKind.TextAttribute => CompletionPriority.TextAttribute,
                CompletionKind.Token => CompletionPriority.Token,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
            Text = text;
            InsertText = text;
            FilterText = text;
            Context = null;
            TextEdit = null;
        }

        public Completion(CompletionKind kind, string text, string? context, string? insertText)
        : this(kind, text)
        {
            InsertText = insertText;
            FilterText = insertText;
            Context = context;
        }
        
        public Completion(CompletionKind kind, string text, TextEdit? textEdit, string? filterText)
            : this(kind, text)
        {
            InsertText = null;
            FilterText = filterText;
            TextEdit = textEdit;
        }

        public override string ToString()
        {
            return $"{nameof(Kind)}: {Kind}, {nameof(Text)}: {Text}, {nameof(Context)}: {Context}, {nameof(InsertText)}: {InsertText}";
        }
    }

    public enum CompletionKind
    {
        Field,
        Pattern,
        Namespace,
        Keyword,
        FilePath,
        DirectoryPath,
        TextAttribute,
        Token
    }

    public static class CompletionPriority
    {
        public static string Field = "0";
        public static string Pattern = "0";
        public static string Namespace = "2";
        public static string Keyword = "2";
        public static string FilePath = "0";
        public static string DirectoryPath = "0";
        public static string TextAttribute = "0";
        public static string Token = "1";
    }
}
