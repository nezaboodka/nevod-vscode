using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nezaboodka.Nevod.Services.Tests
{
    [TestClass]
    public class PatternFinderTests
    {
        [TestMethod]
        public void FindPatternsInSinglePackage()
        {
            // Arrange
            const string patterns = @"
P1 = Word;
P2 = P3 @where {
    P3 = Num;
};";
            LinkedPackageSyntax package = TestHelper.LinkPackageText(patterns);
            PatternFinder patternFinder = new();

            // Act
            PatternSyntax[] foundPatterns = patternFinder.FindPatterns(package);

            // Assert
            Assert.AreEqual(3, foundPatterns.Length);
        }

        [TestMethod]
        public void FindPatternsInMultiplePackages()
        {
            // Arrange
            const string patterns1 = @"
P1 = Word;
P2 = P3 @where {
    P3 = Num;
};";
            const string patterns2 = @"
P4 = Any;
P5 = P4;
P6 = 'Hello';
";
            const string patterns3 = @"
P7 = Space;
";
            LinkedPackageSyntax package1 = TestHelper.LinkPackageText(patterns1);
            LinkedPackageSyntax package2 = TestHelper.LinkPackageText(patterns2);
            LinkedPackageSyntax package3 = TestHelper.LinkPackageText(patterns3);
            PatternFinder patternFinder = new();

            // Act
            PatternSyntax[] foundPatterns = patternFinder.FindPatterns(package1, package2, package3);

            // Assert
            Assert.AreEqual(7, foundPatterns.Length);
        }

        [TestMethod]
        public void FindNoPatterns()
        {
            // Arrange
            string emptyPatterns = string.Empty;
            LinkedPackageSyntax emptyPackage = TestHelper.LinkPackageText(emptyPatterns);
            PatternFinder patternFinder = new();

            // Act
            PatternSyntax[] emptyResult1 = patternFinder.FindPatterns(emptyPackage);
            PatternSyntax[] emptyResult2 = patternFinder.FindPatterns();

            // Assert
            Assert.AreEqual(0, emptyResult1.Length);
            Assert.AreEqual(0, emptyResult2.Length);
        }
    }
}
