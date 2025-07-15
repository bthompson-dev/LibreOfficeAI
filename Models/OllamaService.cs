using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using OllamaSharp.ModelContextProtocol;
using OllamaSharp.ModelContextProtocol.Server;
using OllamaSharp.Models;

namespace LibreOfficeAI.Models
{
    public class OllamaService
    {
        public Chat Chat { get; set; }
        public OllamaApiClient Client { get; }
        public McpClientTool[] AvailableTools { get; set; }

        private string SystemPrompt { get; } =
            "You are a helpful assistant. Use a tool if possible.";

        public bool ToolsLoaded { get; set; } = false;

        public OllamaService()
        {
            var ollamaUri = new Uri("http://localhost:11434");

            // Define an httpClient to allow the timeout to be extended
            var httpClient = new HttpClient()
            {
                BaseAddress = ollamaUri,
                Timeout = TimeSpan.FromMinutes(5),
            };

            string selectedModel = "kitsonk/watt-tool-8B:latest";

            Client = new OllamaApiClient(httpClient, selectedModel);

            Chat = new Chat(Client);
            Chat.Options = new RequestOptions { NumThread = 6 };

            SetupToolEventHandlers();

            _ = Task.Run(async () => await FindTools());
        }

        public void RefreshChat()
        {
            Chat = new Chat(Client);
            SetupToolEventHandlers();
        }

        private void SetupToolEventHandlers()
        {
            // Add event listener for tool calls
            Chat.OnToolCall += (sender, toolCall) =>
            {
                Debug.WriteLine($"Tool called: {toolCall.Function?.Name}");
            };

            // Add event listener for tool results
            Chat.OnToolResult += (sender, result) =>
            {
                Debug.WriteLine($"Tool result: {result.Result}");
            };
        }

        public async Task FindTools()
        {
            // Detailed logging for McpClient
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddDebug().SetMinimumLevel(LogLevel.Debug);
            });

            var options = new McpClientOptions { LoggerFactory = loggerFactory };

            AvailableTools = await Tools.GetFromMcpServers(
                @"C:\Users\ben_t\source\repos\LibreOfficeAI\server_config.json",
                options
            );

            // Log all tools found
            foreach (var tool in AvailableTools)
            {
                Debug.WriteLine(tool?.Function?.Name);
            }

            ToolsLoaded = true;
        }
    }
}
