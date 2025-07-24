using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using OllamaSharp.ModelContextProtocol;
using OllamaSharp.ModelContextProtocol.Server;

namespace LibreOfficeAI.Models
{
    /// <summary>
    /// Provides services for managing and identifying tools available from the MCP server.
    /// </summary>
    /// <remarks>The <see cref="ToolService"/> class is responsible for asynchronously retrieving a list of
    /// tools from the MCP server and determining which tools are needed based on user input through an internal
    /// chat.</remarks>
    public class ToolService
    {
        private McpClientTool[]? AvailableTools { get; set; }
        private HashSet<McpClientTool> NeededTools { get; set; } = [];

        public bool ToolsLoaded { get; set; } = false;

        private Chat InternalChat { get; set; }

        public ToolService(Chat internalChat)
        {
            // Async call to find tools from MCP server
            _ = Task.Run(async () => await FindTools());
            InternalChat = internalChat;
        }

        // Get list of tools from MCP Server
        private async Task FindTools()
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
            //foreach (var tool in AvailableTools)
            //{
            //    Debug.WriteLine(tool?.Function?.Name);
            //}

            ToolsLoaded = true;
        }

        // Use internal chat to decide which tools the user may need
        public async Task<McpClientTool[]?> FindNeededTools(string prompt)
        {
            if (AvailableTools == null)
                return null;

            Debug.WriteLine("Checking for needed tools");

            var neededToolsResponse = new StringBuilder();

            await foreach (var answerToken in InternalChat.SendAsync(prompt))
            {
                neededToolsResponse.Append(answerToken);
            }

            var responseString = neededToolsResponse.ToString();
            Debug.WriteLine(responseString);

            foreach (McpClientTool tool in AvailableTools)
            {
                if (tool.Function?.Name != null && responseString.Contains(tool.Function.Name))
                {
                    NeededTools.Add(tool);
                }
            }

            return [.. NeededTools];
        }

        public void RefreshChat()
        {
            InternalChat.Messages.Clear();
            NeededTools.Clear();
        }
    }
}
