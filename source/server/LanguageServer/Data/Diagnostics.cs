using System;

namespace Nezaboodka.Nevod.LanguageServer
{
    public class PublishDiagnosticsClientCapabilities
    {
        public class TagSupportClientCapabilities
        {
            public DiagnosticTag[]? ValueSet { get; set; }
        }

        public bool? RelatedInformation { get; set; }
        public TagSupportClientCapabilities? TagSupport { get; set; }
        public bool? VersionSupport { get; set; }
        public bool? CodeDescriptionSupport { get; set; }
        public bool? DataSupport { get; set; }
    }
    
    public class PublishDiagnosticsParams 
    {
        public Uri Uri { get; set; }
        public uint? Version { get; set; }
        public Diagnostic[] Diagnostics { get; set; }
        
        public PublishDiagnosticsParams(Uri uri, Diagnostic[] diagnostics)
        {
            Uri = uri;
            Diagnostics = diagnostics;
        }
    }
}
