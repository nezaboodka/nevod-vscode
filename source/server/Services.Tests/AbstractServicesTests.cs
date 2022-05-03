using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nezaboodka.Nevod.Services.Tests
{
    [TestClass]
    public class AbstractServicesTests
    {
        private const string ServicesDirectoryName = "Services";
        private const string RenameDirectoryName = "Rename";
        private const string CompletionsDirectoryName = "Completions";
        private const string FormattingDirectoryName = "Formatting";

        [TestMethod]
        public void GetDefinition()
        {
            // Arrange
            Uri services1Uri = TestHelper.PackageUri(ServicesDirectoryName, "Services1.np");
            Uri services2Uri = TestHelper.PackageUri(ServicesDirectoryName, "Services2.np");
            AbstractServices services = TestHelper.CreateTestServices(ServicesDirectoryName);
            Location p1DefinitionLocation = new(services1Uri, new Range(new Position(0, 0), new Position(0, 2)));
            PointerLocation p1InServices1ReferenceLocation = new(services1Uri, new Position(1, 8));
            PointerLocation p1InServices2ReferenceLocation = new(services2Uri, new Position(2, 5));
            Location f1DefinitionLocation = new(services1Uri, new Range(new Position(0, 3), new Position(0, 5)));
            PointerLocation f1ReferenceLocation = new(services1Uri, new Position(0, 9));
            PointerLocation f1ReferenceInExtractionLocation = new(services1Uri, new Position(2, 16));
            Location f2DefinitionLocation = new(services1Uri, new Range(new Position(2, 3), new Position(2, 5)));
            PointerLocation f2ReferenceLocation = new(services1Uri, new Position(2, 12));
            Location services1Location = new PointerLocation(services1Uri, new Position(0, 0));
            PointerLocation services1RequireLocation = new(services2Uri, new Position(0, 11));
            PointerLocation wordTokenLocation = new(services1Uri, new Position(0, 13));

            // Act
            Location? foundP1InServices1DefinitionLocation = services.GetDefinition(p1InServices1ReferenceLocation);
            Location? foundP1InServices2DefinitionLocation = services.GetDefinition(p1InServices2ReferenceLocation);
            Location? foundF1DefinitionLocation = services.GetDefinition(f1ReferenceLocation);
            Location? foundF1DefinitionForExtractionLocation = services.GetDefinition(f1ReferenceInExtractionLocation);
            Location? foundF2DefinitionLocation = services.GetDefinition(f2ReferenceLocation);
            Location? foundServices1Location = services.GetDefinition(services1RequireLocation);
            Location? foundWordTokenReferenceLocation = services.GetDefinition(wordTokenLocation);

            // Assert
            Assert.AreEqual(p1DefinitionLocation, foundP1InServices1DefinitionLocation);
            Assert.AreEqual(p1DefinitionLocation, foundP1InServices2DefinitionLocation);
            Assert.AreEqual(f1DefinitionLocation, foundF1DefinitionLocation);
            Assert.AreEqual(f1DefinitionLocation, foundF1DefinitionForExtractionLocation);
            Assert.AreEqual(f2DefinitionLocation, foundF2DefinitionLocation);
            Assert.AreEqual(services1Location, foundServices1Location);
            Assert.IsNull(foundWordTokenReferenceLocation);
        }

        [TestMethod]
        public void GetReferences()
        {
            // Arrange
            Uri services1Uri = TestHelper.PackageUri(ServicesDirectoryName, "Services1.np");
            Uri services2Uri = TestHelper.PackageUri(ServicesDirectoryName, "Services2.np");
            AbstractServices services = TestHelper.CreateTestServices(ServicesDirectoryName);
            PointerLocation p1DefinitionLocation = new(services1Uri, new Position(0, 0));
            List<Location> p1ReferenceLocations = new()
            {
                new Location(services1Uri, new Range(new Position(1, 8), new Position(1, 10))),
                new Location(services1Uri, new Range(new Position(2, 9), new Position(2, 11))),
                new Location(services2Uri, new Range(new Position(2, 5), new Position(2, 7)))
            };
            PointerLocation f1DefinitionLocation = new(services1Uri, new Position(0, 4));
            List<Location> f1ReferenceLocations = new()
            {
                new Location(services1Uri, new Range(new Position(0, 9), new Position(0, 11))),
                new Location(services1Uri, new Range(new Position(2, 16), new Position(2, 18)))
            };
            PointerLocation f2DefinitionLocation = new(services1Uri, new Position(2, 3));
            List<Location> f2ReferenceLocations = new()
            {
                new Location(services1Uri, new Range(new Position(2, 12), new Position(2, 14)))
            };
            PointerLocation p2DefinitionLocation = new(services1Uri, new Position(2, 0));
            List<Location> p2ReferenceLocations = new()
            {
                new Location(services2Uri, new Range(new Position(2, 18), new Position(2, 20)))
            };
            PointerLocation p3DefinitionLocation = new(services2Uri, new Position(2, 0));
            PointerLocation wordTokenLocation = new(services1Uri, new Position(0, 13));

            // Act
            IEnumerable<Location>? foundP1ReferenceLocations = services.GetReferences(p1DefinitionLocation);
            IEnumerable<Location>? foundF1ReferenceLocations = services.GetReferences(f1DefinitionLocation);
            IEnumerable<Location>? foundF2ReferenceLocations = services.GetReferences(f2DefinitionLocation);
            IEnumerable<Location>? foundP2ReferenceLocations = services.GetReferences(p2DefinitionLocation);
            IEnumerable<Location>? foundP3ReferenceLocations = services.GetReferences(p3DefinitionLocation);
            IEnumerable<Location>? foundWordTokenReferenceLocations = services.GetReferences(wordTokenLocation);

            // Assert
            CollectionAssert.AreEquivalent(p1ReferenceLocations, foundP1ReferenceLocations?.ToList());
            CollectionAssert.AreEquivalent(f1ReferenceLocations, foundF1ReferenceLocations?.ToList());
            CollectionAssert.AreEquivalent(f2ReferenceLocations, foundF2ReferenceLocations?.ToList());
            CollectionAssert.AreEquivalent(p2ReferenceLocations, foundP2ReferenceLocations?.ToList());
            Assert.IsNotNull(foundP3ReferenceLocations);
            Assert.AreEqual(0, foundP3ReferenceLocations.Count());
            Assert.IsNull(foundWordTokenReferenceLocations);
        }

        [TestMethod]
        public void GetDocumentSymbolsForServices1()
        {
            // Arrange
            Uri uri = TestHelper.PackageUri(ServicesDirectoryName, "Services1.np");
            AbstractServices services = TestHelper.CreateTestServices(ServicesDirectoryName);
            List<Symbol> symbols = new()
            {
                new Symbol(
                    "P1", 
                    "P1", 
                    Enumerable.Empty<Symbol>(), 
                    new Location(uri, new Range(new Position(0, 0), new Position(0, 18))),
                    new Location(uri, new Range(new Position(0, 0), new Position(0, 2)))
                    ),
                new Symbol(
                    "P2",
                    "P2",
                    Enumerable.Empty<Symbol>(),
                    new Location(uri, new Range(new Position(2, 0), new Position(2, 20))),
                    new Location(uri, new Range(new Position(2, 0), new Position(2, 2)))
                    )
            };

            // Act
            IEnumerable<Symbol> foundSymbols = services.GetDocumentSymbols(uri);

            // Assert
            CollectionAssert.AreEqual(symbols, foundSymbols.ToArray());
        }
        
        [TestMethod]
        public void GetDocumentSymbolsForServices2()
        {
            // Arrange
            Uri uri = TestHelper.PackageUri(ServicesDirectoryName, "Services2.np");
            AbstractServices services = TestHelper.CreateTestServices(ServicesDirectoryName);
            List<Symbol> symbols = new()
            {
                new Symbol(
                    "P3", 
                    "P3", 
                    Enumerable.Empty<Symbol>(), 
                    new Location(uri, new Range(new Position(2, 0), new Position(2, 21))),
                    new Location(uri, new Range(new Position(2, 0), new Position(2, 2)))
                ),
                new Symbol(
                    "WithInner",
                    "Namespace.WithInner",
                    new List<Symbol>
                    {
                        new (
                            "Inner",
                            "Namespace.WithInner.Inner",
                            Enumerable.Empty<Symbol>(),
                            new Location(uri, new Range(new Position(7, 8), new Position(7, 20))),
                            new Location(uri, new Range(new Position(7, 8), new Position(7, 13)))
                            )
                    },
                    new Location(uri, new Range(new Position(5, 4), new Position(8, 6))),
                    new Location(uri, new Range(new Position(5, 21), new Position(5, 30)))
                )
            };

            // Act
            IEnumerable<Symbol> foundSymbols = services.GetDocumentSymbols(uri);

            // Assert
            CollectionAssert.AreEqual(symbols, foundSymbols.ToArray());
        }

        [TestMethod]
        public void GetRenameInfoForPattern()
        {
            // Arrange
            Uri patternsUri = TestHelper.PackageUri(RenameDirectoryName, "Patterns1.np");
            TestServices services = TestHelper.CreateTestServices(RenameDirectoryName);
            Location definitionLocation = services.GetIdentifierLocationByAnnotation(patternsUri, "toRename");
            Location ref1Location = services.GetIdentifierLocationByAnnotation(patternsUri, "ref1");
            Location ref2Location = services.GetIdentifierLocationByAnnotation(patternsUri, "ref2");
            RenameInfo definitionRenameInfo = new(definitionLocation.Range, "ToRename");
            RenameInfo ref1RenameInfo = new(ref1Location.Range, "ToRename");
            RenameInfo ref2RenameInfo = new(ref2Location.Range, "ToRename");

            // Act
            RenameInfo? foundDefinitionRenameInfo = services.GetRenameInfo(definitionLocation.StartToPointerLocation());
            RenameInfo? foundRef1RenameInfo = services.GetRenameInfo(ref1Location.StartToPointerLocation());
            RenameInfo? foundRef2RenameInfo = services.GetRenameInfo(ref2Location.StartToPointerLocation());

            // Assert
            Assert.AreEqual(definitionRenameInfo, foundDefinitionRenameInfo);
            Assert.AreEqual(ref1RenameInfo, foundRef1RenameInfo);
            Assert.AreEqual(ref2RenameInfo, foundRef2RenameInfo);
        }

        [TestMethod]
        public void GetRenameInfoForPatternReferenceToUndefinedPattern()
        {
            // Arrange
            Uri patternsUri = TestHelper.PackageUri(RenameDirectoryName, "ReferenceToUndefinedPattern.np");
            TestServices services = TestHelper.CreateTestServices(RenameDirectoryName);
            PointerLocation referenceLocation = services.GetPointerLocationByAnnotation(patternsUri, "ref");

            // Act
            RenameInfo? foundReferenceRenameInfo = services.GetRenameInfo(referenceLocation);

            // Assert
            Assert.IsNull(foundReferenceRenameInfo);
        }

        [TestMethod]
        public void GetRenameInfoForField()
        {
            // Arrange
            Uri fieldsUri = TestHelper.PackageUri(RenameDirectoryName, "Fields.np");
            TestServices services = TestHelper.CreateTestServices(RenameDirectoryName);
            Location fieldLocation = services.GetIdentifierLocationByAnnotation(fieldsUri, "field");
            Location fieldRef1Location = services.GetIdentifierLocationByAnnotation(fieldsUri, "fieldRef1");
            Location fieldRef2Location = services.GetIdentifierLocationByAnnotation(fieldsUri, "fieldRef2");
            Location fieldRef3Location = services.GetIdentifierLocationByAnnotation(fieldsUri, "fieldRef3");
            Location anotherFieldRef1Location = services.GetIdentifierLocationByAnnotation(fieldsUri, "anotherFieldRef1");
            RenameInfo fieldRenameInfo = new(fieldLocation.Range, "Field");
            RenameInfo fieldRef1RenameInfo = new(fieldRef1Location.Range, "Field");
            RenameInfo fieldRef2RenameInfo = new(fieldRef2Location.Range, "Field");
            RenameInfo fieldRef3RenameInfo = new(fieldRef3Location.Range, "Field");
            RenameInfo anotherFieldRef1RenameInfo = new(anotherFieldRef1Location.Range, "AnotherField");

            // Act
            RenameInfo? foundFieldRenameInfo = services.GetRenameInfo(fieldLocation.StartToPointerLocation());
            RenameInfo? foundFieldRef1RenameInfo = services.GetRenameInfo(fieldRef1Location.StartToPointerLocation());
            RenameInfo? foundFieldRef2RenameInfo = services.GetRenameInfo(fieldRef2Location.StartToPointerLocation());
            RenameInfo? foundFieldRef3RenameInfo = services.GetRenameInfo(fieldRef3Location.StartToPointerLocation());
            RenameInfo? foundAnotherFieldRef1RenameInfo = services.GetRenameInfo(anotherFieldRef1Location.StartToPointerLocation());

            // Assert
            Assert.AreEqual(fieldRenameInfo, foundFieldRenameInfo);
            Assert.AreEqual(fieldRef1RenameInfo, foundFieldRef1RenameInfo);
            Assert.AreEqual(fieldRef2RenameInfo, foundFieldRef2RenameInfo);
            Assert.AreEqual(fieldRef3RenameInfo, foundFieldRef3RenameInfo);
            Assert.AreEqual(anotherFieldRef1RenameInfo, foundAnotherFieldRef1RenameInfo);
        }

        [TestMethod]
        public void GetRenameInfoForReferenceToUndefinedField()
        {
            // Arrange
            Uri fieldsUri = TestHelper.PackageUri(RenameDirectoryName, "ReferenceToUndefinedField.np");
            TestServices services = TestHelper.CreateTestServices(RenameDirectoryName);
            PointerLocation ref1Location = services.GetPointerLocationByAnnotation(fieldsUri, "ref1");
            PointerLocation ref2Location = services.GetPointerLocationByAnnotation(fieldsUri, "ref2");
            PointerLocation ref3Location = services.GetPointerLocationByAnnotation(fieldsUri, "ref3");

            // Act
            RenameInfo? foundRef1RenameInfo = services.GetRenameInfo(ref1Location);
            RenameInfo? foundRef2RenameInfo = services.GetRenameInfo(ref2Location);
            RenameInfo? foundRef3RenameInfo = services.GetRenameInfo(ref3Location);

            // Assert
            Assert.IsNull(foundRef1RenameInfo);
            Assert.IsNull(foundRef2RenameInfo);
            Assert.IsNull(foundRef3RenameInfo);
        }

        [TestMethod]
        public void GetRenameInfoForNotRenamableLexeme()
        {
            // Arrange
            Uri lexemesUri = TestHelper.PackageUri(RenameDirectoryName, "NotRenamableLexemes.np");
            TestServices services = TestHelper.CreateTestServices(RenameDirectoryName);
            PointerLocation afterIdentifierLocation = services.GetPointerLocationByAnnotation(lexemesUri, "afterIdentifier");
            PointerLocation equalLocation = services.GetPointerLocationByAnnotation(lexemesUri, "equal");
            PointerLocation tokenLocation = services.GetPointerLocationByAnnotation(lexemesUri, "token");
            PointerLocation textLocation = services.GetPointerLocationByAnnotation(lexemesUri, "text");
            PointerLocation attributeLocation = services.GetPointerLocationByAnnotation(lexemesUri, "attribute");

            // Act
            RenameInfo? foundAfterIdentifierLocation = services.GetRenameInfo(afterIdentifierLocation);
            RenameInfo? foundEqualLocation = services.GetRenameInfo(equalLocation);
            RenameInfo? foundTokenLocation = services.GetRenameInfo(tokenLocation);
            RenameInfo? foundTextLocation = services.GetRenameInfo(textLocation);
            RenameInfo? foundAttributeLocation = services.GetRenameInfo(attributeLocation);

            // Assert
            Assert.IsNull(foundAfterIdentifierLocation);
            Assert.IsNull(foundEqualLocation);
            Assert.IsNull(foundTokenLocation);
            Assert.IsNull(foundTextLocation);
            Assert.IsNull(foundAttributeLocation);
        }

        [TestMethod]
        public void RenamePattern()
        {
            // Arrange
            Uri patterns1Uri = TestHelper.PackageUri(RenameDirectoryName, "Patterns1.np");
            Uri patterns2Uri = TestHelper.PackageUri(RenameDirectoryName, "Patterns2.np");
            TestServices services = TestHelper.CreateTestServices(RenameDirectoryName);
            PointerLocation patternDefinitionLocation = services.GetPointerLocationByAnnotation(patterns1Uri, "toRename");
            PointerLocation ref2Location = services.GetPointerLocationByAnnotation(patterns1Uri, "ref2");
            var renameEdits = new List<TextEdit>
            {
                new(services.GetIdentifierLocationByAnnotation(patterns1Uri, "toRename"), "NewName"),
                new(services.GetIdentifierLocationByAnnotation(patterns1Uri, "ref1"), "NewName"),
                new(services.GetIdentifierLocationByAnnotation(patterns1Uri, "ref2"), "NewName"),
                new(services.GetIdentifierLocationByAnnotation(patterns1Uri, "ref3"), "Pattern.NewName"),
                new(services.GetIdentifierLocationByAnnotation(patterns1Uri, "ref4"), "Namespace.Pattern.NewName"),
                new(services.GetIdentifierLocationByAnnotation(patterns1Uri, "ref5"), "Pattern.NewName"),
                new(services.GetIdentifierLocationByAnnotation(patterns2Uri, "ref6"), "Namespace.Pattern.NewName"),
                new(services.GetIdentifierLocationByAnnotation(patterns1Uri, "refToNested1"), "NewName.Nested"),
                new(services.GetIdentifierLocationByAnnotation(patterns1Uri, "refToNested3"), "NewName.Nested"),
                new(services.GetIdentifierLocationByAnnotation(patterns2Uri, "refToNested4"), "Namespace.Pattern.NewName.Nested"),
                new(services.GetIdentifierLocationByAnnotation(patterns1Uri, "refFromSearchTarget"), "Pattern.NewName"),
                new(services.GetIdentifierLocationByAnnotation(patterns1Uri, "refWithTrivia"), "Pattern.NewName"),
            };

            // Act
            IEnumerable<TextEdit>? foundRenameEdits = services.RenameSymbol(patternDefinitionLocation, "NewName");
            IEnumerable<TextEdit>? foundRenameEditsForReference = services.RenameSymbol(ref2Location, "NewName");

            // Assert
            CollectionAssert.AreEquivalent(renameEdits, foundRenameEdits?.ToArray());
            CollectionAssert.AreEquivalent(renameEdits, foundRenameEditsForReference?.ToArray());
        }

        [TestMethod]
        public void RenamePatternReferenceToUndefinedPattern()
        {
            // Arrange
            Uri patternsUri = TestHelper.PackageUri(RenameDirectoryName, "ReferenceToUndefinedPattern.np");
            TestServices services = TestHelper.CreateTestServices(RenameDirectoryName);
            PointerLocation referenceLocation = services.GetPointerLocationByAnnotation(patternsUri, "ref");

            // Act
            IEnumerable<TextEdit>? foundRenameEdits = services.RenameSymbol(referenceLocation, "NewName");

            // Assert
            Assert.IsNull(foundRenameEdits);
        }

        [TestMethod]
        public void RenameField()
        {
            // Arrange
            Uri fieldsUri = TestHelper.PackageUri(RenameDirectoryName, "Fields.np");
            TestServices services = TestHelper.CreateTestServices(RenameDirectoryName);
            PointerLocation fieldLocation = services.GetPointerLocationByAnnotation(fieldsUri, "field");
            PointerLocation fieldRef1Location = services.GetPointerLocationByAnnotation(fieldsUri, "fieldRef1");
            PointerLocation fieldRef2Location = services.GetPointerLocationByAnnotation(fieldsUri, "fieldRef2");
            PointerLocation fieldRef3Location = services.GetPointerLocationByAnnotation(fieldsUri, "fieldRef3");
            var fieldRenameEdits = new List<TextEdit>
            {
                new(services.GetIdentifierLocationByAnnotation(fieldsUri, "field"), "NewField"),
                new(services.GetIdentifierLocationByAnnotation(fieldsUri, "fieldRef1"), "NewField"),
                new(services.GetIdentifierLocationByAnnotation(fieldsUri, "fieldRef2"), "NewField"),
                new(services.GetIdentifierLocationByAnnotation(fieldsUri, "fieldRef3"), "NewField"),
            };
            PointerLocation anotherFieldLocation = services.GetPointerLocationByAnnotation(fieldsUri, "anotherField");
            PointerLocation anotherFieldRef1Location = services.GetPointerLocationByAnnotation(fieldsUri, "anotherFieldRef1");
            var anotherFieldRenameEdits = new List<TextEdit>
            {
                new(services.GetIdentifierLocationByAnnotation(fieldsUri, "anotherField"), "NewAnotherField"),
                new(services.GetIdentifierLocationByAnnotation(fieldsUri, "anotherFieldRef1"), "NewAnotherField"),
            };

            // Act
            IEnumerable<TextEdit>? foundFieldRenameEdits = services.RenameSymbol(fieldLocation, "NewField");
            IEnumerable<TextEdit>? foundFieldRenameEditsForRef1 = services.RenameSymbol(fieldRef1Location, "NewField");
            IEnumerable<TextEdit>? foundFieldRenameEditsForRef2 = services.RenameSymbol(fieldRef2Location, "NewField");
            IEnumerable<TextEdit>? foundFieldRenameEditsForRef3 = services.RenameSymbol(fieldRef3Location, "NewField");
            IEnumerable<TextEdit>? foundAnotherFieldRenameEdits = services.RenameSymbol(anotherFieldLocation, "NewAnotherField");
            IEnumerable<TextEdit>? foundAnotherFieldRenameEditsForRef1 = services.RenameSymbol(anotherFieldRef1Location, "NewAnotherField");

            // Assert
            CollectionAssert.AreEquivalent(fieldRenameEdits, foundFieldRenameEdits?.ToArray());
            CollectionAssert.AreEquivalent(fieldRenameEdits, foundFieldRenameEditsForRef1?.ToArray());
            CollectionAssert.AreEquivalent(fieldRenameEdits, foundFieldRenameEditsForRef2?.ToArray());
            CollectionAssert.AreEquivalent(fieldRenameEdits, foundFieldRenameEditsForRef3?.ToArray());
            CollectionAssert.AreEquivalent(anotherFieldRenameEdits, foundAnotherFieldRenameEdits?.ToArray());
            CollectionAssert.AreEquivalent(anotherFieldRenameEdits, foundAnotherFieldRenameEditsForRef1?.ToArray());
        }

        [TestMethod]
        public void RenameReferenceToUndefinedField()
        {
            // Arrange
            Uri fieldsUri = TestHelper.PackageUri(RenameDirectoryName, "ReferenceToUndefinedField.np");
            TestServices services = TestHelper.CreateTestServices(RenameDirectoryName);
            PointerLocation ref1Location = services.GetPointerLocationByAnnotation(fieldsUri, "ref1");
            PointerLocation ref2Location = services.GetPointerLocationByAnnotation(fieldsUri, "ref2");
            PointerLocation ref3Location = services.GetPointerLocationByAnnotation(fieldsUri, "ref3");

            // Act
            IEnumerable<TextEdit>? foundRenameEditsForRef1 = services.RenameSymbol(ref1Location, "NewName");
            IEnumerable<TextEdit>? foundRenameEditsForRef2 = services.RenameSymbol(ref2Location, "NewName");
            IEnumerable<TextEdit>? foundRenameEditsForRef3 = services.RenameSymbol(ref3Location, "NewName");

            // Assert
            Assert.IsNull(foundRenameEditsForRef1);
            Assert.IsNull(foundRenameEditsForRef2);
            Assert.IsNull(foundRenameEditsForRef3);
        }

        [TestMethod]
        public void GetPatternCompletionsWithNoInput()
        {
            // Arrange
            Uri patternsUri = TestHelper.PackageUri(CompletionsDirectoryName, "Patterns1.np");
            TestServices services = TestHelper.CreateTestServices(CompletionsDirectoryName);
            PointerLocation noInputLocation = services.GetPointerLocationByAnnotation(patternsUri, "noInput");
            var completions = new List<Completion>
            {
                new(CompletionKind.Namespace, "N1"),
                new(CompletionKind.Namespace, "N1.N2"),
                new(CompletionKind.Namespace, "N3"),
                new(CompletionKind.Pattern, "MasterPattern"),
                new(CompletionKind.Pattern, "Pattern"),
                new(CompletionKind.Pattern, "Nested"),
                new(CompletionKind.Pattern, "Field", "Pattern", "Pattern.Field"),
                new(CompletionKind.Pattern, "Neighbor"),
                new(CompletionKind.Pattern, "Nested", "MasterPattern", "MasterPattern.Nested"),
                new(CompletionKind.Pattern, "InsideUnnamed"),
                new(CompletionKind.Pattern, "PackagePattern1"),
                new(CompletionKind.Pattern, "PackagePattern2", "N2", "N2.PackagePattern2"),
                new(CompletionKind.Pattern, "PackagePattern3"),
                new(CompletionKind.Pattern, "PackagePattern4", "N3", "N3.PackagePattern4"),
                new(CompletionKind.Pattern, "PackagePattern5"),
                new(CompletionKind.Field, "Field")
            };
            completions.AddKeywordAndTokenCompletions();

            // Act
            IEnumerable<Completion>? foundCompletions = services.GetCompletions(noInputLocation);

            // Assert
            CollectionAssert.AreEquivalent(completions, foundCompletions?.ToArray());
        }

        [TestMethod]
        public void GetPatternCompletionsAfterIdentifier()
        {
            // Arrange
            Uri patternsUri = TestHelper.PackageUri(CompletionsDirectoryName, "Patterns1.np");
            TestServices services = TestHelper.CreateTestServices(CompletionsDirectoryName);
            PointerLocation afterIdentifierLocation = services
                .GetIdentifierLocationByAnnotation(patternsUri, "afterIdentifier")
                .EndToPointerLocation();
            var completions = new List<Completion>
            {
                new(CompletionKind.Pattern, "Pattern", "MasterPattern", "MasterPattern.Pattern"),
                new(CompletionKind.Pattern, "Nested", "MasterPattern.Pattern", "MasterPattern.Pattern.Nested"),
                new(CompletionKind.Pattern, "Field", "MasterPattern.Pattern", "MasterPattern.Pattern.Field"),
                new(CompletionKind.Pattern, "Neighbor", "MasterPattern", "MasterPattern.Neighbor"),
                new(CompletionKind.Pattern, "Nested", "MasterPattern", "MasterPattern.Nested"),
                new(CompletionKind.Pattern, "InsideUnnamed", "MasterPattern", "MasterPattern.InsideUnnamed"),
                // Namespace and field names are not filtered on server
                new(CompletionKind.Namespace, "N1"),
                new(CompletionKind.Namespace, "N1.N2"),
                new(CompletionKind.Namespace, "N3"),
                new(CompletionKind.Field, "Field")
            };
            completions.AddKeywordAndTokenCompletions();

            // Act
            IEnumerable<Completion>? foundCompletions = services.GetCompletions(afterIdentifierLocation);

            // Assert
            CollectionAssert.AreEquivalent(completions, foundCompletions?.ToArray());
        }

        [TestMethod]
        public void GetPatternCompletionsInsideIdentifier()
        {
            // Arrange
            Uri patternsUri = TestHelper.PackageUri(CompletionsDirectoryName, "Patterns1.np");
            TestServices services = TestHelper.CreateTestServices(CompletionsDirectoryName);
            PointerLocation insideIdentifierLocation = services
                .GetPointerLocationByAnnotation(patternsUri, "insideIdentifier");
            // Cursor position: N1.|N2.PackagePattern2
            PointerLocation locationAfterN1 = insideIdentifierLocation.IncreaseCharacter(3);
            var completionsAfterN1 = new List<Completion>
            {
                new(CompletionKind.Pattern, "MasterPattern", "N1", "N1.MasterPattern"),
                new(CompletionKind.Pattern, "Pattern", "N1.MasterPattern", "N1.MasterPattern.Pattern"),
                new(CompletionKind.Pattern, "Nested", "N1.MasterPattern.Pattern", "N1.MasterPattern.Pattern.Nested"),
                new(CompletionKind.Pattern, "Field", "N1.MasterPattern.Pattern", "N1.MasterPattern.Pattern.Field"),
                new(CompletionKind.Pattern, "Neighbor", "N1.MasterPattern", "N1.MasterPattern.Neighbor"),
                new(CompletionKind.Pattern, "Nested", "N1.MasterPattern", "N1.MasterPattern.Nested"),
                new(CompletionKind.Pattern, "InsideUnnamed", "N1.MasterPattern", "N1.MasterPattern.InsideUnnamed"),
                new(CompletionKind.Pattern, "PackagePattern1", "N1", "N1.PackagePattern1"),
                new(CompletionKind.Pattern, "PackagePattern2", "N1.N2", "N1.N2.PackagePattern2"),
                new(CompletionKind.Pattern, "PackagePattern3", "N1", "N1.PackagePattern3"),
                // Namespace and field names are not filtered on server
                new(CompletionKind.Namespace, "N1"),
                new(CompletionKind.Namespace, "N1.N2"),
                new(CompletionKind.Namespace, "N3"),
                new(CompletionKind.Field, "Field")
            };
            completionsAfterN1.AddKeywordAndTokenCompletions();
            // Cursor position: N1.N2.|PackagePattern2
            PointerLocation locationAfterN2 = insideIdentifierLocation.IncreaseCharacter(6);
            var completionsAfterN2 = new List<Completion>
            {
                new(CompletionKind.Pattern, "PackagePattern2", "N1.N2", "N1.N2.PackagePattern2"),
                // Namespace and field names are not filtered on server
                new(CompletionKind.Namespace, "N1"),
                new(CompletionKind.Namespace, "N1.N2"),
                new(CompletionKind.Namespace, "N3"),
                new(CompletionKind.Field, "Field")
            };
            completionsAfterN2.AddKeywordAndTokenCompletions();

            // Act
            IEnumerable<Completion>? foundCompletionsAfterN1 = services.GetCompletions(locationAfterN1);
            IEnumerable<Completion>? foundCompletionsAfterN2 = services.GetCompletions(locationAfterN2);

            // Assert
            CollectionAssert.AreEquivalent(completionsAfterN1, foundCompletionsAfterN1?.ToArray());
            CollectionAssert.AreEquivalent(completionsAfterN2, foundCompletionsAfterN2?.ToArray());
        }

        [TestMethod]
        public void GetSearchTargetCompletions()
        {
            // Arrange
            Uri searchTargetsUri = TestHelper.PackageUri(CompletionsDirectoryName, "SearchTargets.np");
            TestServices services = TestHelper.CreateTestServices(CompletionsDirectoryName);
            PointerLocation insideNamespaceLocation =
                services.GetPointerLocationByAnnotation(searchTargetsUri, "insideNamespace");
            var insideNamespaceCompletions = new List<Completion>
            {
                new(CompletionKind.Pattern, "P1", "N2", "N2.P1"),
                new(CompletionKind.Pattern, "P2"),
                new(CompletionKind.Namespace, "N2")
            };
            insideNamespaceCompletions.AddKeywordCompletions();
            PointerLocation globalLocation =
                services.GetPointerLocationByAnnotation(searchTargetsUri, "global");
            var globalCompletions = new List<Completion>
            {
                new(CompletionKind.Pattern, "P1", "N1.N2", "N1.N2.P1"),
                new(CompletionKind.Pattern, "P2", "N1", "N1.P2"),
                new(CompletionKind.Pattern, "P3"),
                new(CompletionKind.Namespace, "N1"),
                new(CompletionKind.Namespace, "N1.N2")
            };
            globalCompletions.AddKeywordCompletions();
            PointerLocation patternSearchLocation =
                services.GetIdentifierLocationByAnnotation(searchTargetsUri, "patternSearch").EndToPointerLocation();
            var patternSearchCompletions = new List<Completion>
            {
                new(CompletionKind.Pattern, "P1", "N1.N2", "N1.N2.P1"),
                new(CompletionKind.Pattern, "P2", "N1", "N1.P2"),
                new(CompletionKind.Namespace, "N1"),
                new(CompletionKind.Namespace, "N1.N2")
            };
            patternSearchCompletions.AddKeywordCompletions();
            // Cursor position: N1.|*
            PointerLocation namespaceSearchLocation =
                services.GetPointerLocationByAnnotation(searchTargetsUri, "namespaceSearch").IncreaseCharacter(3);
            var namespaceSearchCompletions = new List<Completion>
            {
                new(CompletionKind.Pattern, "P1", "N1.N2", "N1.N2.P1"),
                new(CompletionKind.Pattern, "P2", "N1", "N1.P2"),
                new(CompletionKind.Namespace, "N1"),
                new(CompletionKind.Namespace, "N1.N2")
            };
            namespaceSearchCompletions.AddKeywordCompletions();

            // Act
            IEnumerable<Completion>? foundInsideNamespaceCompletions = services.GetCompletions(insideNamespaceLocation);
            IEnumerable<Completion>? foundGlobalCompletions = services.GetCompletions(globalLocation);
            IEnumerable<Completion>? foundPatternSearchCompletions = services.GetCompletions(patternSearchLocation);
            IEnumerable<Completion>? foundNamespaceSearchCompletions = services.GetCompletions(namespaceSearchLocation);

            // Assert
            CollectionAssert.AreEquivalent(insideNamespaceCompletions, foundInsideNamespaceCompletions?.ToArray());
            CollectionAssert.AreEquivalent(globalCompletions, foundGlobalCompletions?.ToArray());
            CollectionAssert.AreEquivalent(patternSearchCompletions, foundPatternSearchCompletions?.ToArray());
            CollectionAssert.AreEquivalent(namespaceSearchCompletions, foundNamespaceSearchCompletions?.ToArray());
        }

        [TestMethod]
        public void GetFieldCompletions()
        {
            // Arrange
            Uri fieldsUri = TestHelper.PackageUri(CompletionsDirectoryName, "Fields.np");
            TestServices services = TestHelper.CreateTestServices(CompletionsDirectoryName);
            PointerLocation extractionFieldLocation =
                services.GetPointerLocationByAnnotation(fieldsUri, "extractionField");
            var extractionFieldCompletions = new List<Completion>
            {
                new(CompletionKind.Field, "Field1"),
                new(CompletionKind.Field, "Field2")
            };
            extractionFieldCompletions.AddKeywordCompletions();
            PointerLocation fieldReferenceLocation =
                services.GetPointerLocationByAnnotation(fieldsUri, "fieldReference");
            var fieldReferenceCompletions = new List<Completion>
            {
                new(CompletionKind.Field, "Field1"),
                new(CompletionKind.Field, "Field2")
            };
            fieldReferenceCompletions.AddKeywordCompletions();
            PointerLocation extractionAfterColonLocation =
                services.GetPointerLocationByAnnotation(fieldsUri, "extractionAfterColon");
            var extractionAfterColonCompletions = new List<Completion>
            {
                new(CompletionKind.Pattern, "P1"),
                new(CompletionKind.Pattern, "P2"),
                new(CompletionKind.Field, "Field1"),
                new(CompletionKind.Field, "Field2")
            };
            extractionAfterColonCompletions.AddKeywordAndTokenCompletions();
            PointerLocation afterOperatorLocation = services.GetPointerLocationByAnnotation(fieldsUri, "afterOperator");
            var afterOperatorCompletions = new List<Completion>
            {
                new(CompletionKind.Pattern, "P1"),
                new(CompletionKind.Pattern, "P2"),
                new(CompletionKind.Field, "Field1"),
                new(CompletionKind.Field, "Field2")
            };
            afterOperatorCompletions.AddKeywordAndTokenCompletions();
            PointerLocation fieldLocation = services.GetPointerLocationByAnnotation(fieldsUri, "field");
            var fieldCompletions = new List<Completion>
            {
                new(CompletionKind.Field, "FieldX"),
                new(CompletionKind.Field, "FieldY")
            };
            fieldCompletions.AddKeywordCompletions();
            PointerLocation fromFieldLocation =
                services.GetPointerLocationByAnnotation(fieldsUri, "fromField");
            var fromFieldCompletions = new List<Completion>
            {
                new(CompletionKind.Field, "Field1"),
                new(CompletionKind.Field, "Field2")
            };
            fromFieldCompletions.AddKeywordCompletions();
            PointerLocation afterOpenBraceLocation = services.GetPointerLocationByAnnotation(fieldsUri, "afterOpenBrace");
            var afterOpenBraceCompletions = new List<Completion>
            {
                new(CompletionKind.Field, "FieldX"),
                new(CompletionKind.Field, "FieldY")
            };
            afterOpenBraceCompletions.AddKeywordCompletions();
            PointerLocation referenceAfterColonLocation =
                services.GetPointerLocationByAnnotation(fieldsUri, "referenceAfterColon");
            var referenceAfterColonCompletions = new List<Completion>
            {
                new(CompletionKind.Field, "Field1"),
                new(CompletionKind.Field, "Field2")
            };
            referenceAfterColonCompletions.AddKeywordCompletions();

            // Act
            IEnumerable<Completion>? foundExtractionFieldCompletions = services.GetCompletions(extractionFieldLocation);
            IEnumerable<Completion>? foundFieldReferenceCompletions = services.GetCompletions(fieldReferenceLocation);
            IEnumerable<Completion>? foundExtractionAfterColonCompletions = services.GetCompletions(extractionAfterColonLocation);
            IEnumerable<Completion>? foundAfterOperatorCompletions = services.GetCompletions(afterOperatorLocation);
            IEnumerable<Completion>? foundFieldCompletions = services.GetCompletions(fieldLocation);
            IEnumerable<Completion>? foundFromFieldCompletions = services.GetCompletions(fromFieldLocation);
            IEnumerable<Completion>? foundAfterOpenBraceCompletions = services.GetCompletions(afterOpenBraceLocation);
            IEnumerable<Completion>? foundReferenceAfterColonCompletions = services.GetCompletions(referenceAfterColonLocation);

            // Assert
            CollectionAssert.AreEquivalent(extractionFieldCompletions, foundExtractionFieldCompletions?.ToArray());
            CollectionAssert.AreEquivalent(fieldReferenceCompletions, foundFieldReferenceCompletions?.ToArray());
            CollectionAssert.AreEquivalent(extractionAfterColonCompletions, foundExtractionAfterColonCompletions?.ToArray());
            CollectionAssert.AreEquivalent(afterOperatorCompletions, foundAfterOperatorCompletions?.ToArray());
            CollectionAssert.AreEquivalent(fieldCompletions, foundFieldCompletions?.ToArray());
            CollectionAssert.AreEquivalent(fromFieldCompletions, foundFromFieldCompletions?.ToArray());
            CollectionAssert.AreEquivalent(afterOpenBraceCompletions, foundAfterOpenBraceCompletions?.ToArray());
            CollectionAssert.AreEquivalent(referenceAfterColonCompletions, foundReferenceAfterColonCompletions?.ToArray());
        }

        [TestMethod]
        public void GetAttributeCompletions()
        {
            // Arrange
            Uri attributesUri = TestHelper.PackageUri(CompletionsDirectoryName, "Attributes.np");
            TestServices services = TestHelper.CreateTestServices(CompletionsDirectoryName);
            PointerLocation textAttributesLocation =
                services.GetPointerLocationByAnnotation(attributesUri, "textAttributes");
            var textAttributesCompletions = new List<Completion>
            {
                new(CompletionKind.TextAttribute, "Alpha"),
                new(CompletionKind.TextAttribute, "Num"),
                new(CompletionKind.TextAttribute, "AlphaNum"),
                new(CompletionKind.TextAttribute, "NumAlpha"),
                new(CompletionKind.TextAttribute, "Lowercase"),
                new(CompletionKind.TextAttribute, "Uppercase"),
                new(CompletionKind.TextAttribute, "TitleCase")
            };
            textAttributesCompletions.AddKeywordCompletions();
            PointerLocation tokenLocation = services.GetPointerLocationByAnnotation(attributesUri, "token");
            List<Completion> tokenCompletions = new();
            tokenCompletions.AddKeywordAndTokenCompletions();
            PointerLocation wordAttributesLocation =
                services.GetPointerLocationByAnnotation(attributesUri, "wordAttributes");
            var wordAttributesCompletions = new List<Completion>
            {
                new(CompletionKind.TextAttribute, "Lowercase"),
                new(CompletionKind.TextAttribute, "Uppercase"),
                new(CompletionKind.TextAttribute, "TitleCase")
            };
            wordAttributesCompletions.AddKeywordCompletions();
            PointerLocation nonWordAttributesLocation =
                services.GetPointerLocationByAnnotation(attributesUri, "nonWordAttributes");
            List<Completion> nonWordAttributesCompletions = new();
            nonWordAttributesCompletions.AddKeywordCompletions();

            // Act
            IEnumerable<Completion>? foundTextAttributesCompletions = services.GetCompletions(textAttributesLocation);
            IEnumerable<Completion>? foundTokenCompletions = services.GetCompletions(tokenLocation);
            IEnumerable<Completion>? foundWordAttributesCompletions = services.GetCompletions(wordAttributesLocation);
            IEnumerable<Completion>? foundNonWordAttributesCompletions = services.GetCompletions(nonWordAttributesLocation);

            // Assert
            CollectionAssert.AreEquivalent(textAttributesCompletions, foundTextAttributesCompletions?.ToArray());
            CollectionAssert.AreEquivalent(tokenCompletions, foundTokenCompletions?.ToArray());
            CollectionAssert.AreEquivalent(wordAttributesCompletions, foundWordAttributesCompletions?.ToArray());
            CollectionAssert.AreEquivalent(nonWordAttributesCompletions, foundNonWordAttributesCompletions?.ToArray());
        }

        [TestMethod]
        public void GetRequireCompletions()
        {
            // Arrange
            Uri fileUri = TestHelper.PackageUri(CompletionsDirectoryName, "RequiredPackages/File1.np");
            TestServices services = TestHelper.CreateTestServices(CompletionsDirectoryName);
            Location require1Location = services.GetStringLocationWithoutQuotesByAnnotation(fileUri, "require1");
            PointerLocation require1LocationEnd = require1Location.EndToPointerLocation();
            var require1Completions = new List<Completion>
            {
                new(CompletionKind.FilePath, "File2.np", new TextEdit(require1Location, "./File2.np"), "./File2.np"),
                new(CompletionKind.FilePath, "CaseSensitive.np", new TextEdit(require1Location, "./CaseSensitive.np"), "./CaseSensitive.np"),
                new(CompletionKind.DirectoryPath, "Folder", new TextEdit(require1Location, "./Folder"), "./Folder")
            };
            Location require2Location = services.GetStringLocationWithoutQuotesByAnnotation(fileUri, "require2");
            PointerLocation require2LocationEnd = require2Location.EndToPointerLocation();
            var require2Completions = new List<Completion>
            {
                new(CompletionKind.FilePath, "File3.np", new TextEdit(require2Location, "./Folder/File3.np"), "./Folder/File3.np")
            };
            Location require3Location = services.GetStringLocationWithoutQuotesByAnnotation(fileUri, "require3");
            PointerLocation require3LocationEnd = require3Location.EndToPointerLocation();
            var require3Completions = new List<Completion>
            {
                new(CompletionKind.FilePath, "File2.np", new TextEdit(require3Location, "./File2.np"), "./File2.np"),
                new(CompletionKind.FilePath, "CaseSensitive.np", new TextEdit(require3Location, "./CaseSensitive.np"), "./CaseSensitive.np"),
                new(CompletionKind.DirectoryPath, "Folder", new TextEdit(require3Location, "./Folder"), "./Folder")
            };

            // Act
            IEnumerable<Completion>? foundRequire1Completions = services.GetCompletions(require1LocationEnd);
            IEnumerable<Completion>? foundRequire2Completions = services.GetCompletions(require2LocationEnd);
            IEnumerable<Completion>? foundRequire3Completions = services.GetCompletions(require3LocationEnd);

            // Assert
            CollectionAssert.AreEquivalent(require1Completions, foundRequire1Completions?.ToArray());
            CollectionAssert.AreEquivalent(require2Completions, foundRequire2Completions?.ToArray());
            CollectionAssert.AreEquivalent(require3Completions, foundRequire3Completions?.ToArray());
        }

        [TestMethod]
        public void GetCaseSensitiveRequireCompletions()
        {
            // Arrange
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            bool isFileSystemCaseSensitive = !(File.Exists(assemblyPath.ToUpper()) && File.Exists(assemblyPath.ToLower()));
            Uri fileUri = TestHelper.PackageUri(CompletionsDirectoryName, "RequiredPackages/CaseSensitive.np");
            TestServices services = TestHelper.CreateTestServices(CompletionsDirectoryName);
            string text = services.GetDocument(fileUri).Text;
            services.CloseDocument(fileUri);
            Uri uriWithChangedCase = TestHelper.PackageUri(CompletionsDirectoryName, "RequiredPackages/CASESENSITIVE.np");
            // Reopen file at path with changed case.
            services.OpenDocument(uriWithChangedCase, text);
            services.RebuildSyntaxInfo();
            Location requireLocation = services.GetStringLocationWithoutQuotesByAnnotation(uriWithChangedCase, "require");
            PointerLocation requireLocationEnd = requireLocation.EndToPointerLocation();
            var requireCompletions = new List<Completion>
            {
                new(CompletionKind.FilePath, "File1.np", new TextEdit(requireLocation, "./File1.np"), "./File1.np"),
                new(CompletionKind.FilePath, "File2.np", new TextEdit(requireLocation, "./File2.np"), "./File2.np"),
                new(CompletionKind.DirectoryPath, "Folder", new TextEdit(requireLocation, "./Folder"), "./Folder")
            };
            if (isFileSystemCaseSensitive)
                requireCompletions.Add(new(CompletionKind.FilePath, "CaseSensitive.np", 
                    new TextEdit(requireLocation, "./CaseSensitive.np"), "./CaseSensitive.np"));

            // Act
            IEnumerable<Completion>? foundRequireCompletions = services.GetCompletions(requireLocationEnd);

            // Assert
            CollectionAssert.AreEquivalent(requireCompletions, foundRequireCompletions?.ToArray());
        }

        [TestMethod]
        public void GetEdgeCasesCompletions()
        {
            // Arrange
            // Use string instead of file not to add blank line in the end.
            string text = @"

Pattern =";
            Uri fileUri = TestHelper.PackageUri(CompletionsDirectoryName, "EdgeCases.np");
            TestServices services = TestHelper.CreateTestServices(CompletionsDirectoryName);
            services.OpenDocument(fileUri, text);
            services.RebuildSyntaxInfo();
            PointerLocation startOfFileLocation = new(fileUri, new Position(0, 0));
            List<Completion> startOfFileCompletions = new();
            startOfFileCompletions.AddKeywordCompletions();
            Document document = services.GetDocument(fileUri);
            Position endOfFIlePosition = document.PositionAt(document.Text.Length);
            PointerLocation endOfFileLocation = new(fileUri, endOfFIlePosition);
            var endOfFileCompletions = new List<Completion>
            {
                new(CompletionKind.Pattern, "Pattern")
            };
            endOfFileCompletions.AddKeywordAndTokenCompletions();

            // Act
            IEnumerable<Completion>? foundStartOfFileCompletions = services.GetCompletions(startOfFileLocation);
            IEnumerable<Completion>? foundEndOfFileCompletions = services.GetCompletions(endOfFileLocation);

            // Assert
            CollectionAssert.AreEquivalent(startOfFileCompletions, foundStartOfFileCompletions?.ToArray());
            CollectionAssert.AreEquivalent(endOfFileCompletions, foundEndOfFileCompletions?.ToArray());
        }

        [TestMethod]
        public void GetCompletionsInsideStringOrComment()
        {
            // Arrange
            Uri stringAndCommentUri = TestHelper.PackageUri(CompletionsDirectoryName, "StringAndComment.np");
            TestServices services = TestHelper.CreateTestServices(CompletionsDirectoryName);
            PointerLocation stringLocation = services
                .GetPointerLocationByAnnotation(stringAndCommentUri, "string")
                .IncreaseCharacter(3);
            PointerLocation multilineCommentLocation = services
                .GetPointerLocationByAnnotation(stringAndCommentUri, "multilineComment")
                .IncreaseCharacter(10);
            PointerLocation singleLineCommentLocation = services
                .GetPointerLocationByAnnotation(stringAndCommentUri, "singleLineComment")
                .IncreaseCharacter(10);

            // Act
            IEnumerable<Completion>? foundStringCompletions = services.GetCompletions(stringLocation);
            IEnumerable<Completion>? foundMultilineCommentCompletions = services.GetCompletions(multilineCommentLocation);
            IEnumerable<Completion>? foundSingleLineCommentCompletions = services.GetCompletions(singleLineCommentLocation);

            // Assert
            Assert.IsNull(foundStringCompletions);
            Assert.IsNull(foundMultilineCommentCompletions);
            Assert.IsNull(foundSingleLineCommentCompletions);
        }

        [TestMethod]
        public void GetKeywordOnlyCompletions()
        {
            // Arrange
            Uri fileUri = TestHelper.PackageUri(CompletionsDirectoryName, "KeywordOnly.np");
            TestServices services = TestHelper.CreateTestServices(CompletionsDirectoryName);
            PointerLocation afterOpenBraceLocation = services
                .GetPointerLocationByAnnotation(fileUri, "afterOpenBrace");
            PointerLocation afterCommaLocation = services
                .GetPointerLocationByAnnotation(fileUri, "afterComma");
            PointerLocation afterOpenCurlyBraceLocation = services
                .GetPointerLocationByAnnotation(fileUri, "afterOpenCurlyBrace");
            List<Completion> keywordOnlyCompletions = new();
            keywordOnlyCompletions.AddKeywordCompletions();

            // Act
            IEnumerable<Completion>? foundAfterOpenBraceCompletions = services.GetCompletions(afterOpenBraceLocation);
            IEnumerable<Completion>? foundAfterCommaCompletions = services.GetCompletions(afterCommaLocation);
            IEnumerable<Completion>? foundAfterOpenCurlyBraceCompletions = services.GetCompletions(afterOpenCurlyBraceLocation);

            // Assert
            CollectionAssert.AreEquivalent(keywordOnlyCompletions, foundAfterOpenBraceCompletions?.ToArray());
            CollectionAssert.AreEquivalent(keywordOnlyCompletions, foundAfterCommaCompletions?.ToArray());
            CollectionAssert.AreEquivalent(keywordOnlyCompletions, foundAfterOpenCurlyBraceCompletions?.ToArray());
        }

        [TestMethod]
        public void GeneralFormatting()
        {
            // Arrange
            Uri fileUri = TestHelper.PackageUri(FormattingDirectoryName, "General.np");
            TestServices services = TestHelper.CreateTestServices(FormattingDirectoryName);
            FormattingOptions options = new FormattingOptions(tabSize: 4, insertSpaces: true, newLine: "\n");
            Document document = services.GetDocument(fileUri);
            string expectedText = @"
@require 'Basic.Url.np';
@require 'Basic.Email.np';

@search Basic.Email.Target;
@search Basic.Url
    .Target;

P1 = Word
@where {
    P2 = Num + Alpha + 'String';
    P3 = {~Word, Num} + ?Space + [1+ Num, 3-4 Alpha] + AlphaNum(3-6, Lowercase) + 'text'!*(Num, 2);
    P4 = Num @where {
        P5 = Word;
    };
};
HtmlTitle(Title) = '<title>' .. Title: [1-20] ~ 'Exclusion' .. '</title>';
WithField(F) = F: Word;
WithExtraction(X, ~Y) = WithField(X: F);
Multi.Part.
    Identifier = Word;
".NormalizeLineFeeds();

            // Act
            TextEdit[] formattingEdits = services.FormatDocument(fileUri, options).ToArray();
            document.Update(formattingEdits);

            // Assert
            Assert.AreEqual(expectedText, document.Text);
        }

        [TestMethod]
        public void FormattingConfiguration()
        {
            // Arrange
            Uri fileUri = TestHelper.PackageUri(FormattingDirectoryName, "Configuration.np");
            TestServices services = TestHelper.CreateTestServices(FormattingDirectoryName);
            FormattingOptions options = new FormattingOptions(tabSize: 4, insertSpaces: true, newLine: "\n");
            Document document = services.GetDocument(fileUri);
            Configuration configuration = new Configuration(new FormattingConfiguration
            {
                PlaceOpenBraceOnNewLine = true,
                InsertSpaceAfterOpeningAndBeforeClosingVariationBraces = true,
                InsertSpaceAfterOpeningAndBeforeClosingSpanBraces = true
            });
            string expectedText = @"
Pattern = Word @where
{
    Inner = Num;
    Variation = { Word, Alpha };
    Span = [ 0+ 'Hello' ];
};
".NormalizeLineFeeds();

            // Act
            services.UpdateConfiguration(configuration);
            TextEdit[] formattingEdits = services.FormatDocument(fileUri, options).ToArray();
            document.Update(formattingEdits);

            // Assert
            Assert.AreEqual(expectedText, document.Text);
        }

        [TestMethod]
        public void FormattingWithTabs()
        {
            // Arrange
            Uri fileUri = TestHelper.PackageUri(FormattingDirectoryName, "WithInner.np");
            TestServices services = TestHelper.CreateTestServices(FormattingDirectoryName);
            FormattingOptions options = new FormattingOptions(tabSize: 4, insertSpaces: false, newLine: "\n");
            Document document = services.GetDocument(fileUri);
            string expectedText =
                "\nPattern = Word @where {\n" +
                "\tInner = Num;\n" +
                "};\n";

            // Act
            TextEdit[] formattingEdits = services.FormatDocument(fileUri, options).ToArray();
            document.Update(formattingEdits);

            // Assert
            Assert.AreEqual(expectedText, document.Text);
        }

        [TestMethod]
        public void RangeFormatting()
        {
            // Arrange
            Uri fileUri = TestHelper.PackageUri(FormattingDirectoryName, "Range.np");
            TestServices services = TestHelper.CreateTestServices(FormattingDirectoryName);
            FormattingOptions options = new FormattingOptions(tabSize: 4, insertSpaces: true, newLine: "\n");
            Document document = services.GetDocument(fileUri);
            // Range of nested patterns
            Range range = new Range(new Position(2, 0), new Position(4, 1));
            string expectedText = @"
Pattern1=Word @where{
    Pattern2 = Num;
    Pattern3 = {Alpha, Num};
};
".NormalizeLineFeeds();

            // Act
            TextEdit[] formattingEdits = services.FormatDocumentRange(fileUri, range, options).ToArray();
            document.Update(formattingEdits);

            // Assert
            Assert.AreEqual(expectedText, document.Text);
        }

        [TestMethod]
        public void FormattingWithComments()
        {
            // Arrange
            Uri fileUri = TestHelper.PackageUri(FormattingDirectoryName, "WithComments.np");
            TestServices services = TestHelper.CreateTestServices(FormattingDirectoryName);
            FormattingOptions options = new FormattingOptions(tabSize: 4, insertSpaces: true, newLine: "\n");
            Document document = services.GetDocument(fileUri);
            string expectedText = @"
P1 = Word
    // Comment
    + Num;
P2 = Word
/* Multiline */ // Another
    + Num;
".NormalizeLineFeeds();

            // Act
            TextEdit[] formattingEdits = services.FormatDocument(fileUri, options).ToArray();
            document.Update(formattingEdits);

            // Assert
            Assert.AreEqual(expectedText, document.Text);
        }

        [TestMethod]
        public void FormattingWithIndentationRecalculation()
        {
            // Arrange
            Uri fileUri = TestHelper.PackageUri(FormattingDirectoryName, "RecalculateIndentation.np");
            TestServices services = TestHelper.CreateTestServices(FormattingDirectoryName);
            FormattingOptions options = new FormattingOptions(tabSize: 4, insertSpaces: true, newLine: "\n");
            Document document = services.GetDocument(fileUri);
            string expectedText = @"
P = Word @where {
    Inner1 = Word
        + Num;
    Inner2 = Alpha;
};
".NormalizeLineFeeds();

            // Act
            TextEdit[] formattingEdits = services.FormatDocument(fileUri, options).ToArray();
            document.Update(formattingEdits);

            // Assert
            Assert.AreEqual(expectedText, document.Text);
        }

        [TestMethod]
        public void FormattingWithNestedStructures()
        {
            // Arrange
            Uri fileUri = TestHelper.PackageUri(FormattingDirectoryName, "NestedStructures.np");
            TestServices services = TestHelper.CreateTestServices(FormattingDirectoryName);
            FormattingOptions options = new FormattingOptions(tabSize: 4, insertSpaces: true, newLine: "\n");
            Document document = services.GetDocument(fileUri);
            string expectedText = @"
Pattern = {
    Word,
    Num +
        Alpha,
    [
        2+ 'string',
        3-6 AlphaNum,
        5 'A' _
            'B'
    ]
};
".NormalizeLineFeeds();

            // Act
            TextEdit[] formattingEdits = services.FormatDocument(fileUri, options).ToArray();
            document.Update(formattingEdits);

            // Assert
            Assert.AreEqual(expectedText, document.Text);
        }

        [TestMethod]
        public void FormattingWithSystemPatterns()
        {
            // Arrange
            Uri fileUri = TestHelper.PackageUri(FormattingDirectoryName, "SystemPatterns.np");
            TestServices services = TestHelper.CreateTestServices(FormattingDirectoryName);
            FormattingOptions options = new FormattingOptions(tabSize: 4, insertSpaces: true, newLine: "\n");
            Document document = services.GetDocument(fileUri);
            string expectedText = @"
Pattern1 = Any;
Pattern2 = Blank;
Pattern3 = WordBreak;
".NormalizeLineFeeds();

            // Act
            TextEdit[] formattingEdits = services.FormatDocument(fileUri, options).ToArray();
            document.Update(formattingEdits);

            // Assert
            Assert.AreEqual(expectedText, document.Text);
        }

        [TestMethod]
        public void FormattingWithNamespace()
        {
            // Arrange
            Uri fileUri = TestHelper.PackageUri(FormattingDirectoryName, "Namespace.np");
            TestServices services = TestHelper.CreateTestServices(FormattingDirectoryName);
            FormattingOptions options = new FormattingOptions(tabSize: 4, insertSpaces: true, newLine: "\n");
            Document document = services.GetDocument(fileUri);
            string expectedText = @"
@namespace N1 {
    P = Word;
    @namespace N2 {
        P = Word @where {
            Nested = Word;
        };
    }
    P2 = Word;
}
".NormalizeLineFeeds();

            // Act
            TextEdit[] formattingEdits = services.FormatDocument(fileUri, options).ToArray();
            document.Update(formattingEdits);

            // Assert
            Assert.AreEqual(expectedText, document.Text);
        }
    }
}
