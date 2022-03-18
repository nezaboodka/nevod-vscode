using System;
using System.IO;

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
            foreach (string filePath in Directory.EnumerateFiles(directoryPath, "*.np"))
            {
                Uri uri = new(filePath);
                string text = File.ReadAllText(filePath);
                base.OpenDocument(uri, text);
            }
            foreach (string childDirectoryPath in Directory.EnumerateDirectories(directoryPath))
                OpenDocumentsInDirectoryRecursively(childDirectoryPath);
        }
    }
}
