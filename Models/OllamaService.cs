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
        private string IntentPrompt { get; set; }

        private readonly string systemPrompt;

        public OllamaService(DocumentService documentService)
        {
            var ollamaUri = new Uri("http://localhost:11434");

            // Define an httpClient to allow the timeout to be extended
            var httpClient = new HttpClient()
            {
                BaseAddress = ollamaUri,
                Timeout = TimeSpan.FromMinutes(5),
            };

            string selectedModel = "qwen3:8b";

            Client = new OllamaApiClient(httpClient, selectedModel);

            systemPrompt = File.ReadAllText(
                "C:\\Users\\ben_t\\source\\repos\\LibreOfficeAI\\SystemPrompt.txt"
            );

            systemPrompt += $"Documents folder: {DocumentService.GetDocumentsPath()}.";

            string documentsString = documentService.GetAvailableDocumentsString();
            if (!string.IsNullOrEmpty(documentsString))
            {
                systemPrompt += $"Available documents in Documents folder: {documentsString}.";
            }

            string documentsInUseString = documentService.GetDocumentsInUseString();
            if (!string.IsNullOrEmpty(documentsInUseString))
            {
                systemPrompt += $"Current documents: {documentsInUseString}.";
            }

            ExternalChat = new Chat(Client, systemPrompt);

            IntentPrompt = File.ReadAllText(
                "C:\\Users\\ben_t\\source\\repos\\LibreOfficeAI\\IntentPrompt.txt"
            );

            InternalChat = new Chat(Client, IntentPrompt);

            // Optionally configure Ollama hyperparameters (e.g. NumCtx, NumBatch, NumThread)
            //Chat.Options = new RequestOptions { UseMmap = false };

            ToolService = new ToolService(InternalChat);

            SetupToolEventHandlers();
        }

        //
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
            };

            // Add event listener for tool results
            ExternalChat.OnToolResult += (sender, result) =>
            {
                Debug.WriteLine($"Tool result: {result.Result}");
            };
        }
    }
}
