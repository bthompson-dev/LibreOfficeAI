using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using OllamaSharp.ModelContextProtocol;
using OllamaSharp.ModelContextProtocol.Server;

namespace LibreOfficeAI.Services
{
    /// <summary>
    /// Provides services for managing and identifying tools available from the MCP server.
    /// </summary>
    /// <remarks>The <see cref="ToolService"/> class is responsible for asynchronously retrieving a list of
    /// tools from the MCP server and determining which tools are needed based on user input through an internal
    /// chat.</remarks>
    public partial class ToolService : ObservableObject
    {
        private readonly ConfigurationService _config;
        private McpClientTool[]? AvailableTools { get; set; }
        private HashSet<McpClientTool> NeededTools { get; set; } = [];

        [ObservableProperty]
        public string? _toolsStatus = null;

        [ObservableProperty]
        public bool _toolsLoaded = false;

        private Chat InternalChat { get; set; }

        public ToolService(Chat internalChat, ConfigurationService config)
        {
            // Async call to find tools from MCP server
            _ = Task.Run(async () => await FindTools());
            InternalChat = internalChat;
            _config = config;
        }

        // Get list of tools from MCP Server
        private async Task FindTools()
        {
            ToolsStatus = "Connecting to LibreOffice...";
            // Detailed logging for McpClient
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddDebug().SetMinimumLevel(LogLevel.Debug);
            });

            var options = new McpClientOptions { LoggerFactory = loggerFactory };

            AvailableTools = await Tools.GetFromMcpServers(_config.ServerConfigPath, options);

            ToolsLoaded = true;
            ToolsStatus = null;
            Debug.WriteLine("Tools loaded");
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
            Debug.WriteLine(InternalChat.Messages.Count);
            // Remove all messages except the first which is the intent prompt
            InternalChat.Messages.RemoveRange(1, InternalChat.Messages.Count - 1);
            Debug.WriteLine(InternalChat.Messages.Count);
            NeededTools.Clear();
        }
    }
}
