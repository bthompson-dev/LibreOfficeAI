using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace LibreOfficeAI.Models
{
    /// <summary>
    /// Provides services for managing and accessing documents within a specified directory.
    /// </summary>
    /// <remarks>The <see cref="DocumentService"/> class initializes by loading all documents from a
    /// configured directory. It offers functionality to retrieve document names and manage documents currently in
    /// use.</remarks>
    public class DocumentService
    {
        public ObservableCollection<Document> DocumentsInUse { get; set; } = [];

        public ObservableCollection<Document> AllDocuments { get; set; } = [];

        private readonly string[] writerExtensions =
        [
            ".odt",
            ".docx",
            ".dotx",
            ".xml",
            ".doc",
            ".dot",
            ".rtf",
            ".wpd",
        ];

        public string DocumentsPath { get; set; }

        public DocumentService()
        {
            DocumentsPath = GetDocumentsPath();

            foreach (string filePath in Directory.GetFiles(DocumentsPath))
            {
                AddDocument(filePath, AllDocuments);
            }
        }

        private static string GetDocumentsPath()
        {
            string settings = File.ReadAllText(
                "C:\\Users\\ben_t\\source\\repos\\LibreOfficeAI\\settings.json"
            );
            JsonDocument jsonSettings = JsonDocument.Parse(settings);
            string? documentsPath = jsonSettings
                .RootElement.GetProperty("documentsFolderPath")
                .GetString();

            if (string.IsNullOrEmpty(documentsPath))
            {
                documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            return documentsPath;
        }

        public string GetAvailableDocumentsString()
        {
            string documentsString = "";
            foreach (Document document in AllDocuments)
            {
                documentsString += document.Name + ", ";
            }

            return documentsString.TrimEnd([' ', ',']);
        }

        public string GetDocumentsInUseString()
        {
            string documentsString = "";
            foreach (Document document in DocumentsInUse)
            {
                documentsString += document.Name + ", ";
            }

            return documentsString.TrimEnd([' ', ',']);
        }

        public void AddDocumentInUse(string filePath)
        {
            AddDocument(filePath, DocumentsInUse);
        }

        public void ClearDocumentsInUse()
        {
            DocumentsInUse.Clear();
        }

        private void AddDocument(string filePath, ObservableCollection<Document> collection)
        {
            FileInfo info = new(filePath);

            // Return if the file path is invalid or document is already added
            if (!info.Exists || collection.Any(doc => doc.Path == filePath))
                return;

            if (writerExtensions.Contains(info.Extension))
            {
                collection.Add(new Document(info.Name, info.Extension, filePath, DocType.Writer));
            }
        }
    }

    public record Document(string Name, string Extension, string Path, DocType DocType) { }

    public enum DocType
    {
        Writer,
        Impress,
    }
}
