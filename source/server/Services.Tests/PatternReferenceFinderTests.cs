using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nezaboodka.Nevod.Services.Tests
{
    [TestClass]
    public class PatternReferenceFinderTests
    {
        private const string ReferencesDirectoryName = "References";
        
        [TestMethod]
        public void FindReferencesInSinglePackage()
        {
            // Arrange
            Uri referencesUri = TestHelper.PackageUri(ReferencesDirectoryName, "References1.np");
            Dictionary<Uri, Document> documents = TestHelper.CreateDocumentsFromDirectory(ReferencesDirectoryName);
            SyntaxInfoTable syntaxInfoTable = TestHelper.CreateSyntaxInfoTable(documents);
            LinkedPackageSyntax package = syntaxInfoTable.GetLinkedPackageSyntaxInfo(referencesUri).Syntax;
            PatternReferenceFinder patternReferenceFinder = new();
            PatternSyntax p2 = ((PatternSyntax)package.Patterns[0]).NestedPatterns[0];

            // Act
            Location[] foundReferences = patternReferenceFinder.FindPatternReferences(p2, syntaxInfoTable, documents, package);

            // Assert
            Assert.AreEqual(6, foundReferences.Length);
        }

        [TestMethod]
        public void FindPatternsInMultiplePackages()
        {
            // Arrange
            Uri referencesUri = TestHelper.PackageUri(ReferencesDirectoryName, "References2.np");
            Dictionary<Uri, Document> documents = TestHelper.CreateDocumentsFromDirectory(ReferencesDirectoryName);
            SyntaxInfoTable syntaxInfoTable = TestHelper.CreateSyntaxInfoTable(documents);
            LinkedPackageSyntax package = syntaxInfoTable.GetLinkedPackageSyntaxInfo(referencesUri).Syntax;
            PatternSyntax p2 = ((PatternSyntax)package.RequiredPackages[0].Package.Patterns[0]).NestedPatterns[0];
            PatternReferenceFinder patternReferenceFinder = new();

            // Act
            Location[] foundReferences = patternReferenceFinder.FindPatternReferences(p2, syntaxInfoTable, documents, 
                package, package.RequiredPackages[0].Package);

            // Assert
            Assert.AreEqual(10, foundReferences.Length);
        }

        [TestMethod]
        public void FindNoReferences()
        {
            // Arrange
            Uri referencesUri = TestHelper.PackageUri(ReferencesDirectoryName, "NoReferences.np");
            Dictionary<Uri, Document> documents = TestHelper.CreateDocumentsFromDirectory(ReferencesDirectoryName);
            SyntaxInfoTable syntaxInfoTable = TestHelper.CreateSyntaxInfoTable(documents);
            LinkedPackageSyntax package = syntaxInfoTable.GetLinkedPackageSyntaxInfo(referencesUri).Syntax;
            var p1 = (PatternSyntax)package.Patterns[0];
            PatternReferenceFinder patternReferenceFinder = new();

            // Act
            Location[] emptyResult1 = patternReferenceFinder.FindPatternReferences(p1, syntaxInfoTable, documents, package);
            Location[] emptyResult2 = patternReferenceFinder.FindPatternReferences(p1, syntaxInfoTable, documents);

            // Assert
            Assert.AreEqual(0, emptyResult1.Length);
            Assert.AreEqual(0, emptyResult2.Length);
        }
    }
}
