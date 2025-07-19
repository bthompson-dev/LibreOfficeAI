using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OllamaSharp.ModelContextProtocol;
using OllamaSharp.ModelContextProtocol.Server;

namespace LibreOfficeAI.Models
{
    public class ToolService
    {
        public McpClientTool[]? AvailableTools { get; set; }

        public bool ToolsLoaded { get; set; } = false;

        public ToolService()
        {
            // Async call to find tools from MCP server
            _ = Task.Run(async () => await FindTools());
        }

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
            foreach (var tool in AvailableTools)
            {
                Debug.WriteLine(tool?.Function?.Name);
            }

            ToolsLoaded = true;
        }
    }
}
