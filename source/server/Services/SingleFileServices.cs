using System;

namespace Nezaboodka.Nevod.Services
{
    public class SingleFileServices : AbstractServices
    {
        public override void OpenDocument(Uri uri, string text)
        {
            base.OpenDocument(uri, text);
            RebuildSyntaxInfo();
        }

        public override void CloseDocument(Uri uri)
        {
            base.CloseDocument(uri);
            RebuildSyntaxInfo();
        }
    }
}
