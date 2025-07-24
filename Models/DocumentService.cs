using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace LibreOfficeAI.Models
{
    public class DocumentService
    {
        public List<Document> DocumentsInUse { get; set; } = [];

        public List<Document> AllDocuments { get; set; } = [];

        public DocumentService()
        {
            string[] writerExtensions =
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

            string folderPath = GetDocumentsPath();

            foreach (string filePath in Directory.GetFiles(folderPath))
            {
                FileInfo info = new(filePath);

                if (writerExtensions.Contains(info.Extension))
                {
                    AllDocuments.Add(
                        new Document(info.Name, info.Extension, filePath, DocType.Writer)
                    );
                }
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
