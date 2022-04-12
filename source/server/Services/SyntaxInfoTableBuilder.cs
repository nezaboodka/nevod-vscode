using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Nezaboodka.Nevod.Services
{
    internal class SyntaxInfoTableBuilder
    {
        private Dictionary<Syntax, ISyntaxInfo<Syntax>> _syntaxInfoTableItems = null!; // Initialized in BuildSyntaxInfoTable
        private SyntaxInfoCollector _syntaxInfoCollector = null!; // Initialized in BuildSyntaxInfoTable
        private readonly Dictionary<Uri, LinkedPackageSyntax> _packageByFileUri = new();

        internal SyntaxInfoTable BuildSyntaxInfoTable(IReadOnlyDictionary<Uri, Document> documents, 
            out Dictionary<Uri, Document> newDocuments)
        {
            _syntaxInfoTableItems = new Dictionary<Syntax, ISyntaxInfo<Syntax>>();
            _syntaxInfoCollector = new SyntaxInfoCollector(_packageByFileUri, documents);
            foreach (Document document in documents.Values)
                if (!_packageByFileUri.ContainsKey(document.Uri))
                    Link(document);
            SyntaxInfoTable result = new(_syntaxInfoTableItems);
            newDocuments = _syntaxInfoCollector.Documents;
            _syntaxInfoCollector = null!;
            _syntaxInfoTableItems = null!;
            _packageByFileUri.Clear();
            return result;
        }

        private void Link(Document document)
        {
            PackageSyntax parsedPackage = document.Package;
            string? baseDirectory = Path.GetDirectoryName(document.Uri.LocalPath);
            LinkedPackageSyntax linkedPackage = _syntaxInfoCollector.Link(parsedPackage, _syntaxInfoTableItems,
                document.Uri.LocalPath, baseDirectory);
            _packageByFileUri.Add(document.Uri, linkedPackage);
        }
    }

    internal class SyntaxInfoCollector : PatternLinker
    {
        private static readonly bool? IsFileSystemCaseSensitive;

        static SyntaxInfoCollector()
        {
            string? mainModulePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (mainModulePath is null)
                IsFileSystemCaseSensitive = null;
            else
            {
                if (File.Exists(mainModulePath.ToLower()) && File.Exists(mainModulePath.ToUpper()))
                    IsFileSystemCaseSensitive = false;
                else
                    IsFileSystemCaseSensitive = true;
            }
        }

        private readonly Dictionary<Uri, LinkedPackageSyntax> _packageByFileUri;
        private Dictionary<Syntax, ISyntaxInfo<Syntax>> _syntaxInfoTableItems = null!; // Initialized in Link
        private Document _document = null!; // Initialized in Link
        private PatternSyntax? _currentPattern;
        private PatternReferenceSyntax? _currentPatternReference;

        internal readonly Dictionary<Uri, Document> Documents;

        public SyntaxInfoCollector(Dictionary<Uri, LinkedPackageSyntax> packageByFileUri, 
            IReadOnlyDictionary<Uri, Document> documents)
            : base(fileContentProvider: null, packageCache: null, IsFileSystemCaseSensitive)
        {
            _packageByFileUri = packageByFileUri;
            Documents = new Dictionary<Uri, Document>(documents); 
        }

        public LinkedPackageSyntax Link(PackageSyntax syntaxTree,
            Dictionary<Syntax, ISyntaxInfo<Syntax>> syntaxInfoTableItems, string documentPath, string? baseDirectory)
        {
            _syntaxInfoTableItems = syntaxInfoTableItems;
            LinkedPackageSyntax result = Link(syntaxTree, baseDirectory, documentPath);
            _document = null!;
            return result;
        }

        public override LinkedPackageSyntax Link(PackageSyntax syntaxTree, string? baseDirectory, string filePath)
        {
            Document saveDocument = _document;
            _document = Documents[new Uri(filePath)];
            LinkedPackageSyntax result = base.Link(syntaxTree, baseDirectory, filePath);
            Location location = GetSyntaxLocation(result);
            SyntaxInfo<LinkedPackageSyntax> info = new(result, location);
            _syntaxInfoTableItems.Add(result, info);
            _document = saveDocument;
            return result;
        }

        public override Syntax? Visit(Syntax? node)
        {
            Syntax? result = null;
            if (node is not null)
                result = !node.TextRange.IsEmpty || node is PackageSyntax ? base.Visit(node) : node /* Ignore Any, Blank, WordBreak children */;
            return result;
        }

        protected override Syntax VisitPatternSearchTarget(PatternSearchTargetSyntax node)
        {
            base.VisitPatternSearchTarget(node);
            // Do not visit PatternReference to avoid incorrect linking. Reference is resolved in VisitPatternSearchTarget.
            if (node.PatternReference is not null)
            {
                Location location = GetSyntaxLocation(node.PatternReference);
                SyntaxInfo<PatternReferenceSyntax> info = new(node.PatternReference, location);
                _syntaxInfoTableItems.Add(node.PatternReference, info);
            }
            return node;
        }

        protected override Syntax VisitPattern(PatternSyntax node)
        {
            PatternSyntax? masterPattern = _currentPattern;
            _currentPattern = node;
            base.VisitPattern(node);
            Location location = GetSyntaxLocation(node);
            PatternSyntaxInfo info = new(node, location, masterPattern);
            _syntaxInfoTableItems.Add(node, info);
            _currentPattern = masterPattern;
            return node;
        }

        protected override Syntax VisitPatternReference(PatternReferenceSyntax node)
        {
            _currentPatternReference = node;
            base.VisitPatternReference(node);
            Visit(node.ExtractionFromFields);
            Location location = GetSyntaxLocation(node);
            SyntaxInfo<PatternReferenceSyntax> info = new(node, location);
            _syntaxInfoTableItems.Add(node, info);
            _currentPatternReference = null;
            return node;
        }

        protected override Syntax VisitField(FieldSyntax node)
        {
            base.VisitField(node);
            Location location = GetSyntaxLocation(node);
            SyntaxInfo<FieldSyntax> info = new(node, location);
            _syntaxInfoTableItems.Add(node, info);
            return node;
        }

        protected override Syntax VisitFieldReference(FieldReferenceSyntax node)
        {
            base.VisitFieldReference(node);
            Location location = GetSyntaxLocation(node);
            FieldReferenceSyntaxInfo info = new(node, location, _currentPattern ?? throw CurrentPatternNotSet());
            _syntaxInfoTableItems.Add(node, info);
            return node;
        }

        protected override Syntax VisitExtraction(ExtractionSyntax node)
        {
            base.VisitExtraction(node);
            Location location = GetSyntaxLocation(node);
            ExtractionSyntaxInfo info = new(node, location, _currentPattern ?? throw CurrentPatternNotSet());
            _syntaxInfoTableItems.Add(node, info);
            return node;
        }

        protected override Syntax VisitExtractionFromField(ExtractionFromFieldSyntax node)
        {
            base.VisitExtractionFromField(node);
            Location location = GetSyntaxLocation(node);
            if (_currentPattern is null)
                throw CurrentPatternNotSet();
            if (_currentPatternReference is null)
                throw CurrentPatternReferenceNotSet();
            ExtractionFromFieldSyntaxInfo info = new(node, location, _currentPattern, _currentPatternReference);
            _syntaxInfoTableItems.Add(node, info);
            return node;
        }

        protected override LinkedPackageSyntax LoadRequiredPackage(string filePath)
        {
            Uri uri = new(filePath);
            if (!_packageByFileUri.TryGetValue(uri, out LinkedPackageSyntax? linkedPackage))
            {
                if (!Documents.TryGetValue(uri, out Document? document))
                {
                    document = new Document(uri, File.ReadAllText(filePath), false); // TODO: cache temporary documents
                    Documents[uri] = document;
                }
                linkedPackage = Link(document.Package, Path.GetDirectoryName(filePath), filePath);
                _packageByFileUri.Add(uri, linkedPackage);
            }
            return linkedPackage;
        }

        private static Exception CurrentPatternNotSet() => new("Current pattern not set");

        private static Exception CurrentPatternReferenceNotSet() => new("Current pattern reference not set");

        private Location GetSyntaxLocation(Syntax node)
        {
            Position start = _document.PositionAt(node.TextRange.Start);
            Position end = _document.PositionAt(node.TextRange.End);
            Location location = new(_document.Uri, new Range(start, end));
            return location;
        }
    }
}
