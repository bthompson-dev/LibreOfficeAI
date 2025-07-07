using System;
using System.Collections.Generic;
using System.Diagnostics;
using OllamaSharp;
using OllamaSharp.ModelContextProtocol;
using OllamaSharp.ModelContextProtocol.Server;

namespace LibreOfficeAI.Models
{
    public class OllamaService
    {
        public Chat Chat { get; set; }
        public OllamaApiClient Client { get; }
        public McpClientTool[] AvailableTools { get; set; }

        public OllamaService()
        {
            var ollamaUri = new Uri("http://localhost:11434");
            Client = new OllamaApiClient(ollamaUri)
            {
                SelectedModel = "kitsonk/watt-tool-8B:latest",
            };

            Chat = new Chat(Client);

            FindTools();
        }

        public void RefreshChat()
        {
            Chat = new Chat(Client);
        }

        public async void FindTools()
        {
            McpClientTool[] AvailableTools = await Tools.GetFromMcpServers(
                @"C:\Users\ben_t\source\repos\LibreOfficeAI\server_config.json"
            );

            foreach (var tool in AvailableTools)
            {
                Debug.WriteLine(tool.Function.Name);
            }
        }
    }
}
