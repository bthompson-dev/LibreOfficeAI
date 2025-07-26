using System;
using System.IO;
using System.Text.Json;

namespace LibreOfficeAI.Models
{
    public class ConfigurationService
    {
        public string DocumentsPath { get; private set; }
        public string SystemPromptPath { get; private set; }
        public string IntentPromptPath { get; private set; }
        public Uri OllamaUri { get; private set; }
        public string SelectedModel { get; private set; }

        public ConfigurationService()
        {
            // Load configurable settings
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

            DocumentsPath = documentsPath;

            // Set hard-coded values
            SystemPromptPath = "C:\\Users\\ben_t\\source\\repos\\LibreOfficeAI\\SystemPrompt.txt";
            IntentPromptPath = "C:\\Users\\ben_t\\source\\repos\\LibreOfficeAI\\IntentPrompt.txt";
            OllamaUri = new Uri("http://localhost:11434");
            SelectedModel = "qwen3:8b";
        }
    }
}
