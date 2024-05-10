using System;
using System.IO;
using System.IO.Enumeration;

namespace Nezaboodka.Nevod.Services
{
    public class WorkspaceServices : AbstractServices
    {
        private readonly string _baseDirectoryPath;

        public WorkspaceServices(string baseDirectoryPath)
        {
            _baseDirectoryPath = baseDirectoryPath;
            OpenDocumentsInDirectoryRecursively(baseDirectoryPath);
            RebuildSyntaxInfo();
        }

        public override void OpenDocument(Uri uri, string text)
        {
            base.OpenDocument(uri, text);
            RebuildSyntaxInfo();
        }

        public override void CloseDocument(Uri uri)
        {
            if (!uri.LocalPath.StartsWith(_baseDirectoryPath))
            {
                base.CloseDocument(uri);
                RebuildSyntaxInfo();
            }
        }

        private void OpenDocumentsInDirectoryRecursively(string directoryPath)
        {
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                AttributesToSkip = FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint,
            };
            foreach (string filePath in Directory.EnumerateFiles(directoryPath, "*.np", options))
            {
                Uri uri = new(filePath);
                string text = File.ReadAllText(filePath);
                base.OpenDocument(uri, text);
            }
        }
    }
}
