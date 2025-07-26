using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.UI.Dispatching;
using OllamaSharp;

namespace LibreOfficeAI.Models
{
    public class OllamaService
    {
        // Delegate for UI thread
        public Action<Action>? RunOnUIThread { get; set; }

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
            systemPrompt += $" Documents folder: {_documentService.DocumentsPath}.";

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
            _documentService.ClearDocumentsInUse();
        }

        private void SetupToolEventHandlers()
        {
            // Add event listener for tool calls
            ExternalChat.OnToolCall += (sender, toolCall) =>
            {
                Debug.WriteLine($"Tool called: {toolCall.Function?.Name}");
            };

            // Add event listener for tool results
            ExternalChat.OnToolResult += (sender, result) =>
            {
                Debug.WriteLine($"Tool result: {result.Result}");

                var arguments = result.ToolCall.Function?.Arguments;

                // Add any documents used to the list of current documents
                if (arguments != null)
                {
                    foreach (var key in new[] { "file_path", "source_path", "target_path" })
                    {
                        if (arguments.TryGetValue(key, out var value))
                        {
                            // value is returned as an object
                            var filePath = value as string ?? value?.ToString();
                            if (!string.IsNullOrEmpty(filePath))
                            {
                                Debug.WriteLine($"Adding file to docs in use: {filePath}");

                                // Updating the UI must take place on UI thread
                                RunOnUIThread?.Invoke(() =>
                                    _documentService.AddDocumentInUse(filePath)
                                );
                            }
                        }
                    }

                    // Specific to create_blank_document function
                    if (arguments.TryGetValue("filename", out var fileName))
                    {
                        // value is returned as an object
                        var filePath = fileName as string ?? fileName?.ToString();

                        // Add the base documents path
                        filePath = $"{_documentService.DocumentsPath}\\{filePath}";

                        Debug.WriteLine($"Adding file to docs in use: {filePath}");

                        // Updating the UI must take place on UI thread
                        RunOnUIThread?.Invoke(() => _documentService.AddDocumentInUse(filePath));
                    }
                }
            };
        }
    }
}
