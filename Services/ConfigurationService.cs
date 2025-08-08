using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json.Linq;

namespace LibreOfficeAI.Services
{
    public class ConfigurationService
    {
        public string DocumentsPath { get; private set; }

        public string?[] PresentationTemplatesPaths { get; private set; } = new string?[2];
        public string SystemPromptPath { get; private set; }
        public string IntentPromptPath { get; private set; }
        public string ServerConfigPath { get; private set; }
        public string OllamaPath { get; private set; }
        public string OllamaModelsDir { get; private set; }
        public Uri OllamaUri { get; private set; }
        public string SelectedModel { get; private set; }

        public ConfigurationService()
        {
#if DEBUG
            // Use hardcoded paths in Debug Mode
            OllamaPath = "C:\\Users\\ben_t\\source\\repos\\LibreOfficeAI\\Ollama\\ollama.exe";
            OllamaModelsDir =
                "C:\\Users\\ben_t\\source\\repos\\LibreOfficeAI\\Ollama\\lib\\models\\";

            string settingsPath = "C:\\Users\\ben_t\\source\\repos\\LibreOfficeAI\\settings.json";

            ServerConfigPath = "C:\\Users\\ben_t\\source\\repos\\LibreOfficeAI\\server_config.json";
            string serverCommand =
                "C:\\Users\\ben_t\\source\\repos\\LibreOfficeAI\\MCPServer\\main.exe";

            SystemPromptPath = "C:\\Users\\ben_t\\source\\repos\\LibreOfficeAI\\SystemPrompt.txt";
            IntentPromptPath = "C:\\Users\\ben_t\\source\\repos\\LibreOfficeAI\\IntentPrompt.txt";
#else
            // Use the deployed directory in Release/Published mode
            OllamaPath = Path.Combine(AppContext.BaseDirectory, "Ollama", "ollama.exe");
            OllamaModelsDir = Path.Combine(AppContext.BaseDirectory, "Ollama", "lib", "models");

            string settingsPath = Path.Combine(AppContext.BaseDirectory, "settings.json");
            string serverCommand = Path.Combine(AppContext.BaseDirectory, "MCPServer", "main.exe");
            ServerConfigPath = Path.Combine(AppContext.BaseDirectory, "server_config.json");

            SystemPromptPath = Path.Combine(AppContext.BaseDirectory, "SystemPrompt.txt");
            IntentPromptPath = Path.Combine(AppContext.BaseDirectory, "IntentPrompt.txt");

#endif
            try
            {
                // Set correct Documents path in settings if it is not already set

                var settingsJson = JObject.Parse(File.ReadAllText(settingsPath));

                // Deserialize the JSON to a dictionary

                var documentsPath = (string?)settingsJson["documentsFolderPath"];

                if (string.IsNullOrEmpty(documentsPath) || !File.Exists(documentsPath))
                {
                    documentsPath = Environment.GetFolderPath(
                        Environment.SpecialFolder.MyDocuments
                    );
                    settingsJson["documentsFolderPath"] = documentsPath;
                    File.WriteAllText(settingsPath, settingsJson.ToString());
                }

                DocumentsPath = documentsPath;

                // Get all possible folders for presentations
                PresentationTemplatesPaths[0] = (string?)settingsJson["presentationTemplatesPath"];
                PresentationTemplatesPaths[1] = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "LibreOffice",
                    "4",
                    "user",
                    "template"
                );

                // Ollama URI
                var ollamaUriString = (string?)settingsJson["ollamaUri"];
                if (string.IsNullOrEmpty(ollamaUriString))
                {
                    throw new InvalidOperationException(
                        "The 'ollamaUri' setting is missing or empty in settings.json."
                    );
                }
                OllamaUri = new Uri(ollamaUriString);

                // Ollama Model
                var selectedModel = (string?)settingsJson["selectedModel"];
                if (string.IsNullOrEmpty(selectedModel))
                {
                    throw new InvalidOperationException(
                        "The 'selectedModel' setting is missing or empty in settings.json."
                    );
                }
                SelectedModel = selectedModel;

                // Add command to run MCP server in server config
                var json = File.ReadAllText(ServerConfigPath);
                var jObject = JObject.Parse(json);

                if (jObject["mcpServers"]?["libreoffice-server"] is JObject libreOfficeServer)
                {
                    var command = (string?)libreOfficeServer["command"];
                    if (string.IsNullOrEmpty(command) || !File.Exists(command))
                    {
                        libreOfficeServer["command"] = serverCommand;
                        File.WriteAllText(ServerConfigPath, jObject.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting up config: {ex}");
            }
        }
    }
}
