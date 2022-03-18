using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Nezaboodka.Nevod.Services.Tests
{
    internal static class TestHelper
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

        internal static string NormalizeLineFeeds(this string s) => s.Replace("\r\n", "\n");

        internal static string AssemblyDirectory()
        {
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(assemblyLocation) ?? throw new Exception("Cannot determine assembly directory");
        }

        internal static Uri DirectoryUri(string directoryName) => 
            new($"{AssemblyDirectory()}/Packages/{directoryName}");

        internal static Uri PackageUri(string directoryName, string fileName) => 
            new($"{AssemblyDirectory()}/Packages/{directoryName}/{fileName}");

        internal static LinkedPackageSyntax LinkPackageText(string patterns)
        {
            PackageSyntax parsedPackage = new SyntaxParser().ParsePackageText(patterns);
            LinkedPackageSyntax linkedPackage = new PatternLinker().Link(parsedPackage, baseDirectory: null, filePath: null);
            return linkedPackage;
        }

        internal static Dictionary<Uri, Document> CreateDocumentsFromDirectory(string directoryName)
        {
            string directoryPath = DirectoryUri(directoryName).LocalPath;
            Dictionary<Uri, Document> documents = new();
            foreach (string filePath in Directory.EnumerateFiles(directoryPath, "*.np"))
            {
                Uri uri = new(filePath);
                string text = File.ReadAllText(filePath);
                documents[uri] = new Document(uri, text, true);
            }
            return documents;
        }

        internal static SyntaxInfoTable CreateSyntaxInfoTable(IReadOnlyDictionary<Uri, Document> documents)
        {
            SyntaxInfoTableBuilder syntaxInfoTableBuilder = new();
            return syntaxInfoTableBuilder.BuildSyntaxInfoTable(documents, out _);
        }

        internal static TestServices CreateTestServices(string folderName)
        {
            TestServices services = new(folderName);
            services.OpenPackagesInServicesDirectory();
            services.RebuildSyntaxInfo();
            return services;
        }

        internal static PointerLocation StartToPointerLocation(this Location location) =>
            new(location.Uri, location.Range.Start);

        internal static PointerLocation EndToPointerLocation(this Location location) =>
            new(location.Uri, location.Range.End);

        internal static PointerLocation IncreaseCharacter(this PointerLocation location, int amount) =>
            new(location.Uri, new Position(location.Position.Line, location.Position.Character + amount));

        internal static void AddKeywordAndTokenCompletions(this List<Completion> completions)
        {
            completions.AddRange(s_englishKeywordCompletions);
            completions.AddRange(s_tokenCompletions);
        }

        internal static void AddKeywordCompletions(this List<Completion> completions)
        {
            completions.AddRange(s_englishKeywordCompletions);
        }
    }

    internal class TestServices : AbstractServices
    {
        private readonly string _folderName;

        public TestServices(string folderName)
        {
            _folderName = folderName;
        }

        public override void OpenDocument(Uri uri, string text) => base.OpenDocument(uri, text.NormalizeLineFeeds());

        public override void UpdateDocument(Uri uri, string text) => base.UpdateDocument(uri, text.NormalizeLineFeeds());

        public void OpenPackagesInServicesDirectory()
        {
            string directory = $"{TestHelper.AssemblyDirectory()}/Packages/{_folderName}";
            OpenDocumentsInDirectoryRecursively(directory);
        }

        private void OpenDocumentsInDirectoryRecursively(string directoryPath)
        {
            foreach (string filePath in Directory.EnumerateFiles(directoryPath, "*.np"))
            {
                Uri uri = new(filePath);
                string text = File.ReadAllText(filePath);
                text.NormalizeLineFeeds();
                OpenDocument(uri, text);
            }
            foreach (string childDirectoryPath in Directory.EnumerateDirectories(directoryPath))
                OpenDocumentsInDirectoryRecursively(childDirectoryPath);
        }

        public new void RebuildSyntaxInfo() => base.RebuildSyntaxInfo();

        public Document GetDocument(Uri uri) => Documents[uri];

        public Location GetIdentifierLocationByAnnotation(Uri uri, string annotation)
        {
            int annotatedOffset = GetAnnotatedOffset(uri, annotation);
            string text = Documents[uri].Text;
            TextRange maxIdentifierRange = new(annotatedOffset, text.Length);
            TextRange identifierRange = Utils.GetMultipartIdentifierRange(text, maxIdentifierRange);
            return Utils.GetTextRangeLocation(uri, identifierRange, Documents);
        }

        public Location GetStringLocationWithoutQuotesByAnnotation(Uri uri, string annotation)
        {
            int annotatedOffset = GetAnnotatedOffset(uri, annotation);
            Document document = Documents[uri];
            string text = Documents[uri].Text;
            TextRange maxStringRange = new(annotatedOffset, text.Length);
            TextRange rangeWithoutStartString = Utils.TrimStartString(text, maxStringRange);
            ReadOnlySpan<char> span = text.AsSpan(annotatedOffset, rangeWithoutStartString.Start - annotatedOffset);
            int stringWithoutQuotesLength = span.TrimStart("'\"").TrimEnd("*!").TrimEnd("'\"").Length;
            int startOffset = annotatedOffset + 1;
            int endOffset = startOffset + stringWithoutQuotesLength;
            TextRange stringWithoutQuotesRange = new(startOffset, endOffset);
            return Utils.GetTextRangeLocation(uri, stringWithoutQuotesRange, Documents);
        }

        public PointerLocation GetPointerLocationByAnnotation(Uri uri, string annotation)
        {
            int annotatedOffset = GetAnnotatedOffset(uri, annotation);
            Document document = Documents[uri];
            Position offsetPosition = document.PositionAt(annotatedOffset);
            return new PointerLocation(uri, offsetPosition);
        }

        private int GetAnnotatedOffset(Uri uri, string annotation)
        {
            string text = Documents[uri].Text;
            if (!annotation.StartsWith('@'))
                annotation = '@' + annotation;
            string annotationComment = $"/*{annotation}*/";
            int annotationStart = text.IndexOf(annotationComment, StringComparison.Ordinal);
            if (annotationStart == -1)
                throw new ArgumentException($"No annotation with name {annotation} found in {uri}", nameof(annotation));
            return annotationStart + annotationComment.Length;
        }
    }
}
