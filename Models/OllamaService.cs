using System;
using System.IO;
using System.Net.Http;
using OllamaSharp;

namespace LibreOfficeAI.Models
{
    public class OllamaService
    {
        public Chat ExternalChat { get; set; }
        private Chat InternalChat { get; set; }
        public OllamaApiClient Client { get; }
        public ToolService ToolService { get; set; }

        private readonly DocumentService _documentService;
        private readonly ConfigurationService _config;
        private string IntentPrompt { get; set; }

        private readonly string systemPrompt;

        public OllamaService(DocumentService documentService, ConfigurationService config)
        {
            _documentService = documentService;
            _config = config;

            // Define an httpClient to allow the timeout to be extended
            var httpClient = new HttpClient()
            {
                BaseAddress = _config.OllamaUri,
                Timeout = TimeSpan.FromMinutes(5),
            };

            Client = new OllamaApiClient(httpClient, _config.SelectedModel);

            systemPrompt = LoadSystemPrompt();

            ExternalChat = new Chat(Client, systemPrompt);

            // Optionally configure Ollama hyperparameters (e.g. NumCtx, NumBatch, NumThread)
            //ExternalChat.Options = new RequestOptions { UseMmap = false };

            IntentPrompt = File.ReadAllText(config.IntentPromptPath);

            InternalChat = new Chat(Client, IntentPrompt);

            ToolService = new ToolService(InternalChat);
        }

        private string LoadSystemPrompt()
        {
            var prompt = File.ReadAllText(_config.SystemPromptPath);

            // Add Document Folder Path
            prompt += $" Documents folder: {_config.DocumentsPath}.";

            // Add list of all available documents
            string documentsString = _documentService.GetAvailableDocumentsString();
            if (!string.IsNullOrEmpty(documentsString))
            {
                prompt += $" Available documents in Documents folder: {documentsString}.";
            }

            return prompt;
        }

        // Create a new chat
        public void RefreshChat()
        {
            ExternalChat = new Chat(Client);
            ToolService.RefreshChat();
            _documentService.ClearDocumentsInUse();
        }
    }
}
