using OllamaSharp;
using System;

namespace LibreOfficeAI.Models
{
    public class OllamaService
    {
        public Chat Chat { get; }
        public OllamaApiClient Client { get; }

        public OllamaService()
        {
            var ollamaUri = new Uri("http://localhost:11434");
            Client = new OllamaApiClient(ollamaUri)
            {
                SelectedModel = "kitsonk/watt-tool-8B:latest"
            };

            Chat = new Chat(Client);
        }
    }
}