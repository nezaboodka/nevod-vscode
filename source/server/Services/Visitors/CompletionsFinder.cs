using System.Collections.Generic;
using System.Linq;

namespace Nezaboodka.Nevod.Services
{
    internal class CompletionsFinder : SyntaxVisitor
    {
        private Dictionary<string, Completion> _patternCompletions = null!; // Initialized in FindCompletions
        private HashSet<string> _fieldCompletions = null!; // Initialized in FindCompletions
        private HashSet<string> _namespaces = null!; // Initialized in FindCompletions
        private PatternSyntax _pattern = null!; // Initialized in FindCompletions
        private PatternSyntax? _parentPattern;
        private LinkedPackageSyntax _package = null!; // Initialized in FindCompletions
        private string _inputValue = null!; // Initialized in FindCompletions
        private string _namespace = null!; // Initialized in FindCompletions
        private CollectionStage _collectionStage;

        private enum CollectionStage
        {
            NestedPatterns,
            NeighborPatterns,
            PackagePatterns,
            PatternsForSearchTarget
        }

        internal Completion[] FindCompletions(PatternSyntaxInfo patternSyntaxInfo, LinkedPackageSyntax packageSyntax, string inputValue)
        {
            _patternCompletions = new Dictionary<string, Completion>();
            _fieldCompletions = new HashSet<string>();
            _namespaces = new HashSet<string>();
            _pattern = patternSyntaxInfo.Syntax;
            _parentPattern = patternSyntaxInfo.MasterPattern;
            _package = packageSyntax;
            _inputValue = inputValue;
            CollectFields();
            CollectNestedPatterns();
            CollectNeighborPatterns();
            CollectPackagePatterns();
            IEnumerable<Completion> patternCompletions = _patternCompletions.Values;
            IEnumerable<Completion> fieldCompletions = _fieldCompletions.Select(f => new Completion(CompletionKind.Field, f));
            IEnumerable<Completion> namespaceCompletions = _namespaces.Where(n => !string.IsNullOrEmpty(n))
                .Select(n => new Completion(CompletionKind.Namespace, n));
            Completion[] result = fieldCompletions.Concat(patternCompletions).Concat(namespaceCompletions).ToArray();
            _patternCompletions = null!;
            _fieldCompletions = null!;
            _namespaces = null!;
            _pattern = null!;
            _package = null!;
            _inputValue = null!;
            _parentPattern = null;
            return result;
        }

        internal Completion[] FindSearchTargetCompletions(LinkedPackageSyntax packageSyntax, string @namespace, string inputValue)
        {
            _patternCompletions = new Dictionary<string, Completion>();
            _fieldCompletions = new HashSet<string>();
            _namespaces = new HashSet<string>();
            _package = packageSyntax;
            _namespace = @namespace;
            _inputValue = inputValue;
            CollectPatternsForSearchTarget();
            IEnumerable<Completion> patternCompletions = _patternCompletions.Values;
            IEnumerable<Completion> namespaceCompletions = GetFilteredNamespacesForSearchTarget()
                .Select(n => new Completion(CompletionKind.Namespace, n));
            Completion[] result = patternCompletions.Concat(namespaceCompletions).ToArray();
            _patternCompletions = null!;
            _fieldCompletions = null!;
            _namespaces = null!;
            _package = null!;
            _namespace = null!;
            _inputValue = null!;
            return result;
        }

        protected override Syntax VisitPattern(PatternSyntax node)
        {
            _namespaces.Add(node.Namespace);
            switch (_collectionStage)
            {
                case CollectionStage.NestedPatterns:
                    if (node.Name is not null)
                    {
                        int fullNamePrefixLength;
                        if (_pattern.Name is not null)
                            fullNamePrefixLength = _pattern.FullName.Length + 1 /* Dot after name */;
                        else
                            fullNamePrefixLength = _pattern.FullName.Length;
                        string shortName = node.FullName[fullNamePrefixLength..];
                        string? shortNameWithMasterPattern = _pattern.Name is null ? null : $"{_pattern.Name}.{shortName}";
                        string? fullNameWithoutCommonNamespace = TryGetNameWithoutCommonNamespace(node, _pattern.Namespace);
                        string fullName = node.FullName;
                        AddPatternCompletion(node.Name, shortName, shortNameWithMasterPattern, 
                            fullNameWithoutCommonNamespace, fullName);
                    }
                    base.VisitPattern(node);
                    break;
                case CollectionStage.NeighborPatterns:
                    if (node.Name is not null && _parentPattern is not null)
                    {
                        int fullNamePrefixLength;
                        if (_parentPattern.Name is not null)
                            fullNamePrefixLength = _parentPattern.FullName.Length + 1 /* Dot after name */;
                        else
                            fullNamePrefixLength = _parentPattern.FullName.Length;
                        string shortName = node.FullName[fullNamePrefixLength..];
                        string? fullNameWithoutCommonNamespace = TryGetNameWithoutCommonNamespace(node, _pattern.Namespace);
                        string fullName = node.FullName;
                        AddPatternCompletion(node.Name, shortName, fullNameWithoutCommonNamespace, fullName);
                    }
                    if (node != _pattern)
                        base.VisitPattern(node);
                    break;
                case CollectionStage.PackagePatterns:
                    if (node.Name is not null)
                    {
                        string? fullNameWithoutCommonNamespace = TryGetNameWithoutCommonNamespace(node, _pattern.Namespace);
                        AddPatternCompletion(node.Name, fullNameWithoutCommonNamespace, node.FullName);
                    }
                    if (node != _parentPattern && /* In case parent pattern is null. */ node != _pattern)
                        base.VisitPattern(node);
                    break;
                case CollectionStage.PatternsForSearchTarget:
                    if (node.Name is not null && (string.IsNullOrEmpty(_namespace) || node.FullName.StartsWith(_namespace + '.')))
                    {                       
                        string fullName;
                        if (string.IsNullOrEmpty(_namespace))
                            fullName = node.FullName;
                        else
                            fullName = node.FullName[(_namespace.Length + 1)..];
                        AddPatternCompletion(node.Name, fullName);
                    }                       
                    base.VisitPattern(node);
                    break;
            }
            return node;
        }

        private string? TryGetNameWithoutCommonNamespace(PatternSyntax pattern, string @namespace)
        {
            string? nameWithoutCommonNamespace = null;
            if (!string.IsNullOrEmpty(@namespace) && !string.IsNullOrEmpty(pattern.Namespace) &&
                (pattern.Namespace == @namespace || pattern.Namespace.StartsWith(@namespace + '.')) &&
                // Full name contains at least two symbols after namespace
                pattern.FullName.Length >= @namespace.Length + 2)
                nameWithoutCommonNamespace = pattern.FullName[(@namespace.Length + 1)..];
            return nameWithoutCommonNamespace;
        }

        private void AddPatternCompletion(string name, params string?[] candidates)
        {
            if (candidates.Length == 0)
                _patternCompletions[name] = new Completion(CompletionKind.Pattern, name);
            else
                foreach (string? candidate in candidates)
                {
                    if (candidate is not null &&
                        !_patternCompletions.ContainsKey(candidate) &&
                        !_fieldCompletions.Contains(candidate) &&
                        // Either short name or full name starts with input value
                        (name.StartsWith(_inputValue) || candidate.StartsWith(_inputValue)))
                        if (candidate.Length == name.Length)
                        {
                            _patternCompletions[name] = new Completion(CompletionKind.Pattern, name);
                            break;
                        }
                        // Candidate name length is at least two symbols longer than short name
                        else if (candidate.Length >= name.Length + 2)
                        {
                            _patternCompletions[candidate] = new Completion(CompletionKind.Pattern, name,
                                candidate[..^(name.Length + 1)], candidate);
                            break;
                        }
                }
        }

        private void CollectNestedPatterns()
        {
            _collectionStage = CollectionStage.NestedPatterns;
            Visit(_pattern.NestedPatterns);
        }

        private void CollectNeighborPatterns()
        {
            if (_parentPattern is not null)
            {
                _collectionStage = CollectionStage.NeighborPatterns;
                Visit(_parentPattern.NestedPatterns);
            }
        }

        private void CollectPackagePatterns()
        {
            _collectionStage = CollectionStage.PackagePatterns;
            Visit(_package.Patterns);
            IEnumerable<PatternSyntax> patternsFromRequiredPackages = _package.RequiredPackages
                .Where(r => r.Package is not null)
                .SelectMany(r => r.Package.Patterns)
                .Cast<PatternSyntax>();
            foreach (PatternSyntax patterns in patternsFromRequiredPackages)
                VisitPattern(patterns);
        }

        private void CollectPatternsForSearchTarget()
        {
            _collectionStage = CollectionStage.PatternsForSearchTarget;
            Visit(_package.Patterns);
            IEnumerable<PatternSyntax> patternsFromRequiredPackages = _package.RequiredPackages
                .Where(r => r.Package is not null)
                .SelectMany(r => r.Package.Patterns)
                .Cast<PatternSyntax>();
            foreach (PatternSyntax patterns in patternsFromRequiredPackages)
                VisitPattern(patterns);
        }

        private IEnumerable<string> GetFilteredNamespacesForSearchTarget()
        {
            IEnumerable<string> filteredNamespaces = _namespaces.Where(n => !string.IsNullOrEmpty(n));
            if (!string.IsNullOrEmpty(_namespace))
                filteredNamespaces = filteredNamespaces
                    .Where(n => n.StartsWith(_namespace + '.'))
                    .Select(n => n[(_namespace.Length + 1)..])
                    .Where(n => !string.IsNullOrEmpty(n));
            return filteredNamespaces;
        }

        private void CollectFields()
        {
            foreach (FieldSyntax field in _pattern.Fields)
                _fieldCompletions.Add(field.Name);
        }
    }
}
