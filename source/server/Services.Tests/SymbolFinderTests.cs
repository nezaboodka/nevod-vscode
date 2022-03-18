using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nezaboodka.Nevod.Services.Tests
{
    [TestClass]
    public class SymbolFinderTests
    {
        private const string SymbolsDirectoryName = "Symbols";

        [TestMethod]
        public void FindSymbolsInSinglePackage()
        {
            // Arrange
            Uri symbolsUri = TestHelper.PackageUri(SymbolsDirectoryName, "Symbols1.np");
            Dictionary<Uri, Document> documents = TestHelper.CreateDocumentsFromDirectory(SymbolsDirectoryName);
            SyntaxInfoTable syntaxInfoTable = TestHelper.CreateSyntaxInfoTable(documents);
            LinkedPackageSyntax package = syntaxInfoTable.GetLinkedPackageSyntaxInfo(symbolsUri).Syntax;
            SymbolFinder symbolFinder = new();

            // Act
            Symbol[] foundSymbols = symbolFinder.FindSymbols(syntaxInfoTable, documents, package);

            // Assert
            Assert.AreEqual(2, foundSymbols.Length);
            Assert.AreEqual(1, foundSymbols[1].Children?.Count());
        }

        [TestMethod]
        public void FindPatternsInMultiplePackages()
        {
            // Arrange
            Uri symbolsUri = TestHelper.PackageUri(SymbolsDirectoryName, "Symbols2.np");
            Dictionary<Uri, Document> documents = TestHelper.CreateDocumentsFromDirectory(SymbolsDirectoryName);
            SyntaxInfoTable syntaxInfoTable = TestHelper.CreateSyntaxInfoTable(documents);
            LinkedPackageSyntax package = syntaxInfoTable.GetLinkedPackageSyntaxInfo(symbolsUri).Syntax;
            SymbolFinder symbolFinder = new();

            // Act
            Symbol[] foundSymbols =
                symbolFinder.FindSymbols(syntaxInfoTable, documents, package, package.RequiredPackages[0].Package);

            // Assert
            Assert.AreEqual(3, foundSymbols.Length);
            Assert.AreEqual(1, foundSymbols[2].Children?.Count());
        }

        [TestMethod]
        public void FindNoSymbols()
        {
            // Arrange
            Uri symbolsUri = TestHelper.PackageUri(SymbolsDirectoryName, "SymbolsEmpty.np");
            Dictionary<Uri, Document> documents = TestHelper.CreateDocumentsFromDirectory(SymbolsDirectoryName);
            SyntaxInfoTable syntaxInfoTable = TestHelper.CreateSyntaxInfoTable(documents);
            LinkedPackageSyntax package = syntaxInfoTable.GetLinkedPackageSyntaxInfo(symbolsUri).Syntax;
            SymbolFinder symbolFinder = new();

            // Act
            Symbol[] emptyResult1 = symbolFinder.FindSymbols(syntaxInfoTable, documents, package);
            Symbol[] emptyResult2 = symbolFinder.FindSymbols(syntaxInfoTable, documents);

            // Assert
            Assert.AreEqual(0, emptyResult1.Length);
            Assert.AreEqual(0, emptyResult2.Length);
        }
    }
}
