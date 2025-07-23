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

        private readonly string documentsPath = Environment.GetFolderPath(
            Environment.SpecialFolder.MyDocuments
        );

        private readonly string systemPrompt;

        public OllamaService()
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

            systemPrompt =
                $"If there are missing parameters for a tool you need to use, make up suitable ones. Do not ask the user for clarification. All documents should be saved and found in {documentsPath}. If you create a new document, use further tools to add content to it. Don't ask for confirmation, just use the tools immediately.";

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
