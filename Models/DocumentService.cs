using System;
using System.Collections.Generic;
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
        public List<Document> DocumentsInUse { get; set; } = [];

        public List<Document> AllDocuments { get; set; } = [];

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

        public DocumentService()
        {
            string folderPath = GetDocumentsPath();

            foreach (string filePath in Directory.GetFiles(folderPath))
            {
                AddDocument(filePath, AllDocuments);
            }
        }

        public static string GetDocumentsPath()
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

        private void AddDocument(string filePath, List<Document> collection)
        {
            FileInfo info = new(filePath);

            if (writerExtensions.Contains(info.Extension))
            {
                collection.Add(new Document(info.Name, info.Extension, filePath, DocType.Writer));
            }
        }
    }

    public class Document(string name, string extension, string path, DocType docType)
    {
        public string Name { get; set; } = name;

        public string Extension { get; set; } = extension;
        public string Path { get; set; } = path;
        public DocType DocType { get; set; } = docType;
    }

    public enum DocType
    {
        Writer,
        Impress,
    }
}
