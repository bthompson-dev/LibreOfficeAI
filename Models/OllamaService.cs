using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using OllamaSharp.ModelContextProtocol;
using OllamaSharp.ModelContextProtocol.Server;

namespace LibreOfficeAI.Models
{
    public class OllamaService
    {
        public Chat ExternalChat { get; set; }
        private Chat InternalChat { get; set; }
        public OllamaApiClient Client { get; }
        public ToolService ToolService { get; set; }

        public OllamaService()
        {
            var ollamaUri = new Uri("http://localhost:11434");

            // Define an httpClient to allow the timeout to be extended
            var httpClient = new HttpClient()
            {
                BaseAddress = ollamaUri,
                Timeout = TimeSpan.FromMinutes(5),
            };

            string selectedModel = "llama3.2";

            Client = new OllamaApiClient(httpClient, selectedModel);

            ExternalChat = new Chat(Client);

            var intentPrompt = File.ReadAllText(
                "C:\\Users\\ben_t\\source\\repos\\LibreOfficeAI\\IntentPrompt.txt"
            );

            InternalChat = new Chat(Client, intentPrompt);

            // Optionally configure Ollama hyperparameters (e.g. NumCtx, NumBatch, NumThread)
            //Chat.Options = new RequestOptions { UseMmap = false };

            ToolService = new ToolService();

            SetupToolEventHandlers();
        }

        // Use internal chat to decide which tools the user may need
        public async Task<McpClientTool[]> FindNeededTools(string prompt)
        {
            Debug.WriteLine("Request sent to find tools");

            var neededToolsResponse = new StringBuilder();

            await foreach (var answerToken in InternalChat.SendAsync(prompt))
            {
                neededToolsResponse.Append(answerToken);
            }

            var responseString = neededToolsResponse.ToString();
            Debug.WriteLine(responseString);

            var toolsToInclude = new List<McpClientTool>();

            foreach (McpClientTool tool in ToolService.AvailableTools)
            {
                if (responseString.Contains(tool.Function.Name))
                {
                    toolsToInclude.Add(tool);
                }
            }

            return [.. toolsToInclude];
        }

        public void RefreshExternalChat()
        {
            ExternalChat = new Chat(Client);
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
