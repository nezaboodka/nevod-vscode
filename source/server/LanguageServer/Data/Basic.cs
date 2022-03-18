using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Nezaboodka.Nevod.LanguageServer
{
    public class RegularExpressionsClientCapabilities
    {
        public string Engine { get; set; }
        public string? Version { get; set; }

        public RegularExpressionsClientCapabilities(string engine) => Engine = engine;
    }

    public readonly struct Position
    {
        public int Line { get; }
        public int Character { get; }

        public Position(int line, int character)
        {
            Line = line;
            Character = character;
        }

        public static implicit operator Position(Services.Position position) => new Position(position.Line, position.Character);
        public static implicit operator Services.Position(Position position) => new Services.Position(position.Line, position.Character);
    }

    public readonly struct Range
    {
        public Position Start { get; }
        public Position End { get; }

        public Range(Position start, Position end)
        {
            Start = start;
            End = end;
        }

        public static implicit operator Range(Services.Range range) => new Range(range.Start, range.End);
    }

    public readonly struct Location
    {
        public Uri Uri { get; }
        public Range Range { get; }

        public Location(Uri uri, Range range)
        {
            Uri = uri;
            Range = range;
        }

        public static implicit operator Location(Services.Location location) => new(location.Uri, location.Range);
    }
    
    public class Diagnostic
    {
        public Range Range { get; set; }
        public DiagnosticSeverity? Severity { get; set; }
        public string? Code { get; set; }
        public CodeDescription? CodeDescription { get; set; }
        public string? Source { get; set; }
        public string Message { get; set; }
        public DiagnosticTag[]? Tags { get; set; }
        public DiagnosticRelatedInformation[]? RelatedInformation { get; set; }
        public object? Data { get; set; }
        
        public Diagnostic(Range range, string message)
        {
            Range = range;
            Message = message;
        }
    }

    public enum DiagnosticSeverity 
    {
        Error = 1,
        Warning,
        Information,
        Hint
    }
    
    public enum DiagnosticTag
    {
        Unnecessary = 1,
        Deprecated
    }
    
    public class DiagnosticRelatedInformation
    {
        public Location Location { get; set; }
        public string Message { get; set; }
        
        public DiagnosticRelatedInformation(Location location, string message)
        {
            Location = location;
            Message = message;
        }
    }
    
    public class  CodeDescription
    {
        public Uri Href { get; set; }

        public CodeDescription(Uri href)
        {
            Href = href;
        }
    }

    public readonly struct Command
    {
        public string Title { get; }
        [JsonProperty("command")] public string Identifier { get; }
        public object[]? Arguments { get; }

        public Command(string title, string identifier, object[]? arguments)
        {
            Title = title;
            Identifier = identifier;
            Arguments = arguments;
        }
    }

    public class TextEdit
    {
        public Range Range { get; set; }
        public string NewText { get; set; }

        public TextEdit(Range range, string newText)
        {
            Range = range;
            NewText = newText;
        }

        public static implicit operator TextEdit(Services.TextEdit textEdit) =>
            new(textEdit.Location.Range, textEdit.NewText);
    }
    
    public class ChangeAnnotation 
    {
        public string Label { get; set; }
        public bool? NeedsConfirmation { get; set; }
        public string? Description { get; set; }
        
        public ChangeAnnotation(string label)
        {
            Label = label;
        }   
    }

    public class TextDocumentEdit
    {
        public OptionalVersionedTextDocumentIdentifier TextDocument { get; set; }
        public TextEdit[] Edits { get; set; }

        public TextDocumentEdit(OptionalVersionedTextDocumentIdentifier textDocument, TextEdit[] edits)
        {
            TextDocument = textDocument;
            Edits = edits;
        }
    }

    public class WorkspaceEdit
    {
        public Dictionary<Uri, TextEdit[]>? Changes { get; set; }
        public TextDocumentEdit[]? DocumentChanges { get; set; }
        public Dictionary<string, ChangeAnnotation>? ChangeAnnotations { get; set; }
    }

    public class WorkspaceEditClientCapabilities
    {
        public class ClientChangeAnnotationSupport
        {
            public bool? GroupsOnLabel { get; set; }
        }

        public bool? DocumentChanges { get; set; }
        public ResourceOperationKind[]? ResourceOperations { get; set; }
        public FailureHandlingKind? FailureHandling { get; set; }
        public bool? NormalizesLineEndings { get; set; }
        public ClientChangeAnnotationSupport? ChangeAnnotationSupport { get; set; }
    }

    public enum ResourceOperationKind
    {
        Create,
        Rename,
        Delete
    }

    public enum FailureHandlingKind
    {
        Abort,
        Transactional,
        TextOnlyTransactional,
        Undo
    }

    public class TextDocumentIdentifier
    {
        public Uri Uri { get; set; }

        public TextDocumentIdentifier(Uri uri) => Uri = uri;
    }

    public class OptionalVersionedTextDocumentIdentifier : TextDocumentIdentifier
    {
        public int? Version { get; set; }

        public OptionalVersionedTextDocumentIdentifier(Uri uri) : base(uri) { }
    }

    public class TextDocumentItem
    {
        public Uri Uri { get; set; }
        public string LanguageId { get; set; }
        public int Version { get; set; }
        public string Text { get; set; }

        public TextDocumentItem(Uri uri, string languageId, int version, string text)
        {
            Uri = uri;
            LanguageId = languageId;
            Version = version;
            Text = text;
        }
    }

    public class VersionedTextDocumentIdentifier : TextDocumentIdentifier
    {
        public int Version { get; set; }

        public VersionedTextDocumentIdentifier(Uri uri, int version) : base(uri) => Version = version;
    }

    public class TextDocumentPositionParams
    {
        public TextDocumentIdentifier TextDocument { get; set; }
        public Position Position { get; set; }

        public TextDocumentPositionParams(TextDocumentIdentifier textDocument) => TextDocument = textDocument;
    }

    public enum MarkupKind
    {
        PlainText,
        Markdown
    }

    public class MarkdownClientCapabilities
    {
        public string Parser { get; set; }
        public string? Version { get; set; }

        public MarkdownClientCapabilities(string parser) => Parser = parser;
    }

    public class WorkDoneProgressParams
    {
        public object? WorkDoneToken { get; set; }
    }

    public class WorkDoneProgressOptions
    {
        public bool? WorkDoneProgress { get; set; }
    }

    public enum TraceValue
    {
        Off,
        Messages,
        Verbose
    }

    class RegistrationParams 
    {
        public Registration[] Registrations { get; set; } 

        public RegistrationParams(Registration[] registrations)
        {
            Registrations = registrations;
        }
    }

    public class Registration 
    {
        public string Id { get; set; }
        public string Method { get; set; }
        public object? RegisterOptions { get; set; }

        public Registration(string id, string method)
        {
            Id = id;
            Method = method;
        }
    }
}
