using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nezaboodka.Nevod.Services.Tests
{
    [TestClass]
    public class SyntaxResolverTests
    {
        [TestMethod]
        public void ResolveSyntax()
        {
            // Arrange
            string patterns = @"
P1 = P2 @where {
    P2(A) = A: Word;
} ; ".NormalizeLineFeeds();
            SyntaxResolver resolver = new();
            LinkedPackageSyntax package = TestHelper.LinkPackageText(patterns);
            var p1Pattern = (PatternSyntax)package.Patterns[0];
            Syntax p2Reference = ((PatternSyntax)package.Patterns[0]).Body;
            PatternSyntax p2Pattern = p1Pattern.NestedPatterns[0];
            FieldSyntax p2FieldA = p2Pattern.Fields[0];

            // Act
            const int beforePackageOffset = 0;
            Syntax? resolvedBeforePackage = resolver.Resolve(package, beforePackageOffset);
            const int p1Offset = 1;
            Syntax? resolvedP1 = resolver.Resolve(package, p1Offset);
            const int p1TriviaOffset = 42;
            Syntax? resolvedAfterP1 = resolver.Resolve(package, p1TriviaOffset);
            const int p2ReferenceOffset = 6;
            Syntax? resolvedP2Reference = resolver.Resolve(package, p2ReferenceOffset);
            const int p2Offset = 22;
            Syntax? resolvedP2 = resolver.Resolve(package, p2Offset);
            const int p2FieldAOffset = 25;
            Syntax? resolvedP2FieldA = resolver.Resolve(package, p2FieldAOffset);
            const int parenAfterP2FieldAOffset = 26;
            Syntax? resolvedParenAfterP2FieldA = resolver.Resolve(package, parenAfterP2FieldAOffset);

            // Assert
            Assert.IsNull(resolvedBeforePackage);
            Assert.AreEqual(p1Pattern, resolvedP1);
            Assert.AreEqual(p1Pattern, resolvedAfterP1);
            Assert.AreEqual(p2Reference, resolvedP2Reference);
            Assert.AreEqual(p2Pattern, resolvedP2);
            Assert.AreEqual(p2FieldA, resolvedP2FieldA);
            Assert.AreEqual(p2Pattern, resolvedParenAfterP2FieldA);
        }

        [TestMethod]
        public void ResolveSyntaxFromChildren()
        {
            // Arrange
            string patterns = @"P1(X) = { X: Num, Word + Alpha };".NormalizeLineFeeds();
            SyntaxResolver resolver = new();
            LinkedPackageSyntax package = TestHelper.LinkPackageText(patterns);
            var variation = (VariationSyntax)((PatternSyntax)package.Patterns[0]).Body;
            variation.CreateChildren(patterns);
            variation.Elements[0].CreateChildren(patterns);
            variation.Elements[1].CreateChildren(patterns);
            var openCurlyBrace = (Lexeme)variation.Children[0];
            var colon = (Lexeme)variation.Elements[0].Children[1];
            var comma = (Lexeme)variation.Children[2];
            var plus = (Lexeme)variation.Elements[1].Children[1];
            var closeCurlyBrace = (Lexeme)variation.Children[4];
            
            // Act
            const int openCurlyBraceOffset = 9;
            Syntax? openCurlyBraceParent = resolver.Resolve(package, openCurlyBraceOffset);
            Assert.IsNotNull(openCurlyBraceParent);
            Syntax? resolvedOpenCurlyBrace = resolver.ResolveFromChildren(openCurlyBraceParent.Children, openCurlyBraceOffset);
            const int colonOffset = 12;
            Syntax? colonParent = resolver.Resolve(package, colonOffset);
            Assert.IsNotNull(colonParent);
            Syntax? resolvedColon = resolver.ResolveFromChildren(colonParent.Children, colonOffset);
            const int commaOffset = 17;
            Syntax? commaParent = resolver.Resolve(package, commaOffset);
            Assert.IsNotNull(commaParent);
            Syntax? resolvedComma = resolver.ResolveFromChildren(commaParent.Children, commaOffset);
            const int plusOffset = 24;
            Syntax? plusParent = resolver.Resolve(package, plusOffset);
            Assert.IsNotNull(plusParent);
            Syntax? resolvedPlus = resolver.ResolveFromChildren(plusParent.Children, plusOffset);
            const int closeCurlyBraceOffset = 31;
            Syntax? closeCurlyBraceParent = resolver.Resolve(package, closeCurlyBraceOffset);
            Assert.IsNotNull(closeCurlyBraceParent);
            Syntax? resolvedCloseCurlyBrace = resolver.ResolveFromChildren(closeCurlyBraceParent.Children, closeCurlyBraceOffset);
            
            // Assert
            Assert.AreEqual(openCurlyBrace, resolvedOpenCurlyBrace);
            Assert.AreEqual(colon, resolvedColon);
            Assert.AreEqual(comma, resolvedComma);
            Assert.AreEqual(plus, resolvedPlus);
            Assert.AreEqual(closeCurlyBrace, resolvedCloseCurlyBrace);
        }
    }
}
