using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nezaboodka.Nevod.Services.Tests
{
    [TestClass]
    public class UtilsTests
    {
        [TestMethod]
        public void TestTrimStartSingleLineComment()
        {
            // Arrange
            string textEmpty = string.Empty;
            const string textWithComment = "// Some comment\n// Some comment\nNezaboodka";
            const string textWithCommentOnly = "// Some comment";
            const string textWithoutComment = "Nezaboodka";
            const string textWithWindowsLineFeed = "// Some comment\r\nNezaboodka";

            // Act
            TextRange trimmedTextEmpty = Utils.TrimStartMultilineComment(textEmpty, GetRangeFromText(textEmpty));
            TextRange trimmedTextWithComment =
                Utils.TrimStartSingleLineComment(textWithComment, GetRangeFromText(textWithComment));
            TextRange trimmedTextWithCommentOnly =
                Utils.TrimStartSingleLineComment(textWithCommentOnly, GetRangeFromText(textWithCommentOnly));
            TextRange trimmedTextWithoutComment =
                Utils.TrimStartSingleLineComment(textWithoutComment, GetRangeFromText(textWithoutComment));
            TextRange trimmedTextWithWindowsLineFeed =
                Utils.TrimStartSingleLineComment(textWithWindowsLineFeed, GetRangeFromText(textWithWindowsLineFeed));
            void PassOutOfLeftBoundTextRange() => Utils.TrimStartSingleLineComment(string.Empty, new TextRange(-1, 0));
            void PassOutOfRightBoundTextRange() => Utils.TrimStartSingleLineComment(string.Empty, new TextRange(0, 1));

            // Assert
            Assert.AreEqual(string.Empty, GetTextFromRange(textEmpty, trimmedTextEmpty));
            Assert.AreEqual("Nezaboodka", GetTextFromRange(textWithComment, trimmedTextWithComment));
            Assert.AreEqual(string.Empty, GetTextFromRange(textWithCommentOnly, trimmedTextWithCommentOnly));
            Assert.AreEqual("Nezaboodka", GetTextFromRange(textWithoutComment, trimmedTextWithoutComment));
            Assert.AreEqual("Nezaboodka", GetTextFromRange(textWithWindowsLineFeed, trimmedTextWithWindowsLineFeed));
            Assert.ThrowsException<ArgumentException>(PassOutOfLeftBoundTextRange);
            Assert.ThrowsException<ArgumentException>(PassOutOfRightBoundTextRange);
        }

        [TestMethod]
        public void TestTrimStartMultilineComment()
        {
            // Arrange
            string textEmpty = string.Empty;
            const string textWithComment = @"/* Some comment *//* Some comment */Nezaboodka";
            const string textWithCommentOnly = "/* Some comment */";
            const string textWithoutComment = "Nezaboodka";
            const string textWithUnterminatedComment = "/* Some comment";

            // Act
            TextRange trimmedTextEmpty = Utils.TrimStartMultilineComment(textEmpty, GetRangeFromText(textEmpty));
            TextRange trimmedTextWithComment =
                Utils.TrimStartMultilineComment(textWithComment, GetRangeFromText(textWithComment));
            TextRange trimmedTextWithCommentOnly =
                Utils.TrimStartMultilineComment(textWithCommentOnly, GetRangeFromText(textWithCommentOnly));
            TextRange trimmedTextWithoutComment =
                Utils.TrimStartMultilineComment(textWithoutComment, GetRangeFromText(textWithoutComment));
            TextRange textWithUnterminatedCommentRange = GetRangeFromText(textWithUnterminatedComment);
            TextRange trimmedTextWithUnterminatedComment = 
                Utils.TrimStartMultilineComment(textWithUnterminatedComment, textWithUnterminatedCommentRange);
            void PassOutOfLeftBoundTextRange() => Utils.TrimStartSingleLineComment(string.Empty, new TextRange(-1, 0));
            void PassOutOfRightBoundTextRange() => Utils.TrimStartSingleLineComment(string.Empty, new TextRange(0, 1));

            // Assert
            Assert.AreEqual(string.Empty, GetTextFromRange(textEmpty, trimmedTextEmpty));
            Assert.AreEqual("Nezaboodka", GetTextFromRange(textWithComment, trimmedTextWithComment));
            Assert.AreEqual(string.Empty, GetTextFromRange(textWithCommentOnly, trimmedTextWithCommentOnly));
            Assert.AreEqual("Nezaboodka", GetTextFromRange(textWithoutComment, trimmedTextWithoutComment));
            Assert.AreEqual(new TextRange(textWithUnterminatedCommentRange.End, textWithUnterminatedCommentRange.End),
                trimmedTextWithUnterminatedComment);
            Assert.ThrowsException<ArgumentException>(PassOutOfLeftBoundTextRange);
            Assert.ThrowsException<ArgumentException>(PassOutOfRightBoundTextRange);
        }

        [TestMethod]
        public void TestTrimStartTrivia()
        {
            // Arrange
            string textEmpty = string.Empty;
            const string textWithoutStartTrivia = "Nezaboodka";
            const string textWithStartTrivia = @"
// Some single-line comment

/*
   Some multiline comment 
*/

   Nezaboodka";

            // Act
            TextRange trimmedTextEmpty = Utils.TrimStartTrivia(textEmpty, GetRangeFromText(textEmpty));
            TextRange trimmedTextWithoutStartTrivia =
                Utils.TrimStartTrivia(textWithoutStartTrivia, GetRangeFromText(textWithoutStartTrivia));
            TextRange trimmedTextWithStartTrivia =
                Utils.TrimStartTrivia(textWithStartTrivia, GetRangeFromText(textWithStartTrivia));
            void PassOutOfLeftBoundTextRange() => Utils.TrimStartSingleLineComment(string.Empty, new TextRange(-1, 0));
            void PassOutOfRightBoundTextRange() => Utils.TrimStartSingleLineComment(string.Empty, new TextRange(0, 1));

            // Assert
            Assert.AreEqual(string.Empty, GetTextFromRange(textEmpty, trimmedTextEmpty));
            Assert.AreEqual("Nezaboodka", GetTextFromRange(textWithoutStartTrivia, trimmedTextWithoutStartTrivia));
            Assert.AreEqual("Nezaboodka", GetTextFromRange(textWithStartTrivia, trimmedTextWithStartTrivia));
            Assert.ThrowsException<ArgumentException>(PassOutOfLeftBoundTextRange);
            Assert.ThrowsException<ArgumentException>(PassOutOfRightBoundTextRange);
        }

        [TestMethod]
        public void TestTrimEndTrivia()
        {
            // Arrange
            string textEmpty = string.Empty;
            const string textWithoutTrivia = "Nezaboodka";
            const string textWithEndTrivia = @"Nezaboodka
// Some single-line comment

/*
   Some multiline comment 
*/

   ";
            const string textWithMultipleTrivia = @"Nezaboodka1
// Some single-line comment
Nezaboodka2
// Some single-line comment

/*
   Some multiline comment 
*/

   ";
            const string textWithTriviaLikeStrings = @"
""string contains // single-line comment""
Nezaboodka
""string contains /*
    multiline comment
*/""
// Some single-line comment
/*
   Some multiline comment
*/
";
            const string textWithTokensWithoutGaps = "\"string\";";
            TextRange stringBeforeSemicolonRange = new(0, 8);

            // Act
            TextRange trimmedTextEmpty = Utils.TrimEndTrivia(textEmpty, GetRangeFromText(textEmpty));
            TextRange trimmedTextWithoutTrivia =
                Utils.TrimEndTrivia(textWithoutTrivia, GetRangeFromText(textWithoutTrivia));
            TextRange trimmedTextWithEndTrivia =
                Utils.TrimEndTrivia(textWithEndTrivia, GetRangeFromText(textWithEndTrivia));
            TextRange trimmedTextWithMultipleTrivia =
                Utils.TrimEndTrivia(textWithMultipleTrivia, GetRangeFromText(textWithMultipleTrivia));
            TextRange trimmedTextWithTriviaLikeStrings = Utils.TrimEndTrivia(textWithTriviaLikeStrings,
                GetRangeFromText(textWithTriviaLikeStrings));
            TextRange trimmedTextWithTokensWithoutGaps = Utils.TrimEndTrivia(textWithTokensWithoutGaps,
                stringBeforeSemicolonRange);
            void PassOutOfLeftBoundTextRange() => Utils.TrimStartSingleLineComment(string.Empty, new TextRange(-1, 0));
            void PassOutOfRightBoundTextRange() => Utils.TrimStartSingleLineComment(string.Empty, new TextRange(0, 1));

            // Assert
            Assert.AreEqual(string.Empty, GetTextFromRange(textEmpty, trimmedTextEmpty));
            Assert.AreEqual("Nezaboodka", GetTextFromRange(textWithoutTrivia, trimmedTextWithoutTrivia));
            Assert.AreEqual("Nezaboodka", GetTextFromRange(textWithEndTrivia, trimmedTextWithEndTrivia));
            Assert.AreEqual(@"Nezaboodka1
// Some single-line comment
Nezaboodka2", GetTextFromRange(textWithMultipleTrivia, trimmedTextWithMultipleTrivia));
            Assert.AreEqual(@"
""string contains // single-line comment""
Nezaboodka
""string contains /*
    multiline comment
*/""", GetTextFromRange(textWithTriviaLikeStrings, trimmedTextWithTriviaLikeStrings));
            Assert.AreEqual("\"string\"", GetTextFromRange(textWithTokensWithoutGaps, 
                trimmedTextWithTokensWithoutGaps));
            Assert.ThrowsException<ArgumentException>(PassOutOfLeftBoundTextRange);
            Assert.ThrowsException<ArgumentException>(PassOutOfRightBoundTextRange);
        }

        [TestMethod]
        public void TestTrimStartKeywords()
        {
            // Arrange
            const string textWithoutTrivia = "@search@pattern P1 = Word;";
            const string textWithTrivia = @"
// Some single-line comment
@search
/*
Some multiline comment
*/
@pattern
// Some single-line comment
/* 
Some multiline comment 
*/
P1 = Word;";

            // Act
            TextRange trimmedTextWithoutTrivia =
                Utils.TrimStartKeywords(textWithoutTrivia, GetRangeFromText(textWithoutTrivia));
            TextRange trimmedTextWithTrivia = Utils.TrimStartKeywords(textWithTrivia, GetRangeFromText(textWithTrivia));
            void PassOutOfLeftBoundTextRange() => Utils.TrimStartSingleLineComment(string.Empty, new TextRange(-1, 0));
            void PassOutOfRightBoundTextRange() => Utils.TrimStartSingleLineComment(string.Empty, new TextRange(0, 1));

            // Assert
            Assert.AreEqual("P1 = Word;", GetTextFromRange(textWithoutTrivia, trimmedTextWithoutTrivia));
            Assert.AreEqual("P1 = Word;", GetTextFromRange(textWithTrivia, trimmedTextWithTrivia));
            Assert.ThrowsException<ArgumentException>(PassOutOfLeftBoundTextRange);
            Assert.ThrowsException<ArgumentException>(PassOutOfRightBoundTextRange);
        }

        private static TextRange GetRangeFromText(string text) => new(0, text.Length);

        private static string GetTextFromRange(string text, TextRange range) => text[range.Start..range.End];
    }
}
