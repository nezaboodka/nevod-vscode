using System;

namespace Nezaboodka.Nevod.LanguageServer
{
    public class WorkspaceFoldersServerCapabilities
    {
        public bool? Supported { get; set; }
        public object ChangeNotifications { get; set; }

        public WorkspaceFoldersServerCapabilities(object changeNotifications) =>
            ChangeNotifications = changeNotifications;
    }

    public class WorkspaceFolder
    {
        public Uri Uri { get; set; }
        public string Name { get; set; }

        public WorkspaceFolder(Uri uri, string name)
        {
            Uri = uri;
            Name = name;
        }
    }

    public class DidChangeConfigurationClientCapabilities
    {
        public bool? DynamicConfiguration { get; set; }
    }

    public class DidChangeWatchedFilesClientCapabilities
    {
        public bool? DynamicConfiguration { get; set; }
    }

    public class WorkspaceSymbolClientCapabilities
    {
        public class SymbolKindClientCapabilities
        {
            SymbolKind[]? ValueSet { get; set; }
        }

        public class TagSupportClientCapabilities
        {
            SymbolTag[]? ValueSet { get; set; }
        }

        public bool? DynamicRegistration { get; set; }
        public SymbolKindClientCapabilities? SymbolKind { get; set; }
        public TagSupportClientCapabilities? TagSupport { get; set; }
    }

    public class WorkspaceSymbolParams : WorkDoneProgressOptions
    {
        public string Query { get; set; }

        public WorkspaceSymbolParams(string query) => Query = query;
    }

    public class ExecuteCommandClientCapabilities
    {
        public bool? DynamicRegistration { get; set; }
    }

    public class ExecuteCommandOptions : WorkDoneProgressOptions
    {
        public string[] Commands { get; set; }

        public ExecuteCommandOptions(string[] commands) => Commands = commands;
    }

    public class FileOperationRegistrationOptions
    {
        public FileOperationFilter[] Filters { get; set; }

        public FileOperationRegistrationOptions(FileOperationFilter[] filters) => Filters = filters;
    }

    public enum FileOperationPatternKind
    {
        File,
        Folder
    }

    public class FileOperationPatternOptions
    {
        public bool? IgnoreCase { get; set; }
    }

    public class FileOperationPattern
    {
        public string Glob { get; set; }
        public FileOperationPatternKind? Matches { get; set; }
        public FileOperationPatternOptions? Options { get; set; }

        public FileOperationPattern(string glob) => Glob = glob;
    }

    public class FileOperationFilter
    {
        public string? Scheme { get; set; }
        public FileOperationPattern Pattern { get; set; }

        public FileOperationFilter(FileOperationPattern pattern) => Pattern = pattern;
    }

    public class SymbolInformation
    {
        public string Name { get; set; }
        public SymbolKind Kind { get; set; }
        public SymbolTag[]? Tags { get; set; }
        public bool? Deprecated { get; set; }
        public Location Location { get; set; }
        public string? ContainerName { get; set; }

        public SymbolInformation(string name, SymbolKind kind, Location location)
        {
            Name = name;
            Kind = kind;
            Location = location;
        }
    }

    class DidChangeWatchedFilesRegistrationOptions 
    {
        public FileSystemWatcher[] Watchers { get; set; }

        public DidChangeWatchedFilesRegistrationOptions(FileSystemWatcher[] watchers)
        {
            Watchers = watchers;
        }
    }

    class FileSystemWatcher 
    {
        public string GlobPattern { get; set; }
        public WatchKind? Kind { get; set; }

        public FileSystemWatcher(string globPattern)
        {
            GlobPattern = globPattern;
        }
    }

    [Flags]
    enum WatchKind 
    {
        Create = 1,
        Change = 2,
        Delete = 4
    }

    class DidChangeWatchedFilesParams 
    {
        public FileEvent[] Changes { get; set; }

        public DidChangeWatchedFilesParams(FileEvent[] changes)
        {
            Changes = changes;
        }
    }

    class FileEvent 
    {
        public Uri Uri { get; set; }
        public FileChangeType Type { get; set; }

        public FileEvent(Uri uri, FileChangeType type)
        {
            Uri = uri;
            Type = type;
        }
    }

    enum FileChangeType 
    {
        Created = 1,
        Changed,
        Deleted
    }

    class ConfigurationParams
    {
        public ConfigurationItem[] Items { get; set; }

        public ConfigurationParams(ConfigurationItem[] items)
        {
            Items = items;
        }
    }

    class ConfigurationItem
    {
        public Uri? ScopeUri { get; set; }
        public string? Section { get; set; }
    }
}
