using System;

namespace Nezaboodka.Nevod.Services
{
    internal class Document
    {
        private static readonly SyntaxParser s_parser = new();

        private LineMap? _lineMap;
        private PackageSyntax? _package;

        private LineMap LineMap
        {
            get
            {
                _lineMap ??= new LineMap(Text);
                return _lineMap;
            }
        }
        
        internal PackageSyntax Package
        {
            get
            {
                _package ??= s_parser.ParsePackageText(Text);
                return _package;
            }
        }

        internal Uri Uri { get; }
        internal string Text { get; private set; }
        internal bool IsTrackedByServer { get; set; }

        internal Document(Uri uri, string text, bool isTrackedByServer)
        {
            Uri = uri;
            Text = text;
            IsTrackedByServer = isTrackedByServer;
        }

        internal void Update(string text)
        {
            Text = text;
            _lineMap = null;
            _package = null;
        }

        internal Position PositionAt(int offset) => LineMap.PositionAt(offset);

        internal int OffsetAt(Position position) => LineMap.OffsetAt(position);
    }
}
