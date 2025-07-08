using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using OllamaSharp;
using OllamaSharp.ModelContextProtocol;
using OllamaSharp.ModelContextProtocol.Server;
using Windows.Media.Protection.PlayReady;

namespace LibreOfficeAI.Models
{
    public class OllamaService
    {
        public Chat Chat { get; set; }
        public OllamaApiClient Client { get; }
        public object[] AvailableTools { get; set; }

        public OllamaService()
        {
            var ollamaUri = new Uri("http://localhost:11434");
            Client = new OllamaApiClient(ollamaUri)
            {
                SelectedModel = "kitsonk/watt-tool-8B:latest",
            };

            string systemPrompt = "Always use a tool.";

            Chat = new Chat(Client, systemPrompt);

            FindTools();
        }

        public void RefreshChat()
        {
            Chat = new Chat(Client);
        }

        public async void FindTools()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddDebug().SetMinimumLevel(LogLevel.Debug);
            });

            var modelInfo = await Client.ShowModelAsync("kitsonk/watt-tool-8B:latest");

            // Checking the capabilities of the model
            Debug.WriteLine($"Model capabilities: {string.Join(", ", modelInfo.Capabilities)}");

            var options = new McpClientOptions { LoggerFactory = loggerFactory };

            AvailableTools = await Tools.GetFromMcpServers(
                @"C:\Users\ben_t\source\repos\LibreOfficeAI\server_config.json",
                options
            );

            foreach (var tool in AvailableTools)
            {
                Debug.WriteLine(tool);
            }
        }
    }
}
