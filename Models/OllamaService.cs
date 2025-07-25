using System;
using System.Diagnostics;
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
        private string IntentPrompt { get; set; }

        private readonly string systemPrompt;

        public OllamaService(DocumentService documentService)
        {
            _documentService = documentService;

            var ollamaUri = new Uri("http://localhost:11434");

            // Define an httpClient to allow the timeout to be extended
            var httpClient = new HttpClient()
            {
                BaseAddress = ollamaUri,
                Timeout = TimeSpan.FromMinutes(5),
            };

            // AI Model
            string selectedModel = "qwen3:8b";

            Client = new OllamaApiClient(httpClient, selectedModel);

            systemPrompt = File.ReadAllText(
                "C:\\Users\\ben_t\\source\\repos\\LibreOfficeAI\\SystemPrompt.txt"
            );

            // Add Document Folder Path
            systemPrompt += $" Documents folder: {DocumentService.GetDocumentsPath()}.";

            // Add list of all available documents
            string documentsString = _documentService.GetAvailableDocumentsString();
            if (!string.IsNullOrEmpty(documentsString))
            {
                systemPrompt += $" Available documents in Documents folder: {documentsString}.";
            }

            ExternalChat = new Chat(Client, systemPrompt);

            // Optionally configure Ollama hyperparameters (e.g. NumCtx, NumBatch, NumThread)
            //ExternalChat.Options = new RequestOptions { UseMmap = false };

            IntentPrompt = File.ReadAllText(
                "C:\\Users\\ben_t\\source\\repos\\LibreOfficeAI\\IntentPrompt.txt"
            );

            InternalChat = new Chat(Client, IntentPrompt);

            ToolService = new ToolService(InternalChat);

            SetupToolEventHandlers();
        }

        // Create a new chat
        public void RefreshChat()
        {
            ExternalChat = new Chat(Client);
            ToolService.RefreshChat();
            SetupToolEventHandlers();
        }

        private void SetupToolEventHandlers()
        {
            // Add event listener for tool calls
            ExternalChat.OnToolCall += (sender, toolCall) =>
            {
                Debug.WriteLine($"Tool called: {toolCall.Function?.Name}");

                var arguments = toolCall.Function?.Arguments;

                // Add any documents used to the list of current documents
                if (arguments != null)
                {
                    if (arguments.TryGetValue("file_path", out var value))
                    {
                        // value is returned as an object
                        var filePath = value as string ?? value?.ToString();
                        if (!string.IsNullOrEmpty(filePath))
                        {
                            Debug.WriteLine($"Adding file to docs in use: {filePath}");
                            _documentService.AddDocumentInUse(filePath);
                        }
                    }
                }
            };

            // Add event listener for tool results
            ExternalChat.OnToolResult += (sender, result) =>
            {
                Debug.WriteLine($"Tool result: {result.Result}");
            };
        }
    }
}
