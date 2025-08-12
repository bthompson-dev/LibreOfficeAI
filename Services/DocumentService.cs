﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace LibreOfficeAI.Services
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

        private ObservableCollection<Document> AllDocuments { get; set; } = [];

        private List<string?> PresentationTemplates { get; set; } = [];

        public readonly string[] writerExtensions =
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

        public readonly string[] impressExtensions =
        [
            ".odp",
            ".pptx",
            ".ppsx",
            ".ppmx",
            ".potx",
            ".pomx",
            ".ppt",
            ".pps",
            ".ppm",
            ".pot",
            ".pom",
        ];

        public DocumentService(ConfigurationService config)
        {
            foreach (string filePath in Directory.GetFiles(config.DocumentsPath))
            {
                AddDocument(filePath, AllDocuments);
            }

            var templatePaths = config
                .defaultTemplatesPaths.Concat(config.AddedPresentationTemplatesPaths)
                .ToList();
            var allTemplateFiles = new List<string>();

            foreach (var folderPath in templatePaths)
            {
                if (Directory.Exists(folderPath))
                {
                    var otpFiles = Directory.GetFiles(
                        folderPath,
                        "*.otp",
                        SearchOption.AllDirectories
                    );
                    allTemplateFiles.AddRange(otpFiles);
                }
            }

            PresentationTemplates = [.. allTemplateFiles.Select(Path.GetFileNameWithoutExtension)];
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

        public string GetPresentationTemplatesString()
        {
            string presentationsTemplatesString = "";
            foreach (string? templateName in PresentationTemplates)
            {
                if (!string.IsNullOrEmpty(templateName))
                {
                    presentationsTemplatesString += templateName + ", ";
                }
            }

            return presentationsTemplatesString.TrimEnd([' ', ',']);
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

            if (impressExtensions.Contains(info.Extension))
            {
                collection.Add(new Document(info.Name, info.Extension, filePath, DocType.Impress));
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
