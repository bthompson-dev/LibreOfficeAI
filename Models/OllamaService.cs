using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using OllamaSharp;

namespace LibreOfficeAI.Models
{
    public partial class OllamaService : IDisposable
    {
        private Process? _ollamaProcess;
        public Chat ExternalChat { get; set; }
        private Chat InternalChat { get; set; }
        public OllamaApiClient Client { get; }
        public ToolService ToolService { get; set; }

        private readonly DocumentService _documentService;
        private readonly ConfigurationService _config;
        private string IntentPrompt { get; set; }

        private readonly string systemPrompt;

        public OllamaService(DocumentService documentService, ConfigurationService config)
        {
            _documentService = documentService;
            _config = config;

            // Define an httpClient to allow the timeout to be extended
            var httpClient = new HttpClient()
            {
                BaseAddress = _config.OllamaUri,
                Timeout = TimeSpan.FromMinutes(5),
            };

            Client = new OllamaApiClient(httpClient, _config.SelectedModel);

            systemPrompt = LoadSystemPrompt();

            ExternalChat = new Chat(Client, systemPrompt);

            // Optionally configure Ollama hyperparameters (e.g. NumCtx, NumBatch, NumThread)
            //ExternalChat.Options = new RequestOptions { UseMmap = false };

            IntentPrompt = File.ReadAllText(config.IntentPromptPath);

            InternalChat = new Chat(Client, IntentPrompt);

            ToolService = new ToolService(InternalChat);
        }

        private string LoadSystemPrompt()
        {
            var prompt = File.ReadAllText(_config.SystemPromptPath);

            // Add Document Folder Path
            prompt += $" Documents folder: {_config.DocumentsPath}.";

            // Add list of all available documents
            string documentsString = _documentService.GetAvailableDocumentsString();
            if (!string.IsNullOrEmpty(documentsString))
            {
                prompt += $" Available documents in Documents folder: {documentsString}.";
            }

            string presentationTemplatesString = _documentService.GetPresentationTemplatesString();
            if (!string.IsNullOrEmpty(presentationTemplatesString))
            {
                prompt += $" Presentation Template Names: {presentationTemplatesString}.";
                prompt +=
                    " If the user gives you a template, make sure the name exactly matches one of these.";
            }

            return prompt;
        }

        // Create a new chat
        public void RefreshChat()
        {
            string systemPrompt = LoadSystemPrompt();

            ExternalChat = new Chat(Client, systemPrompt);
            ToolService.RefreshChat();
            _documentService.ClearDocumentsInUse();
        }

        public async Task StartAsync()
        {
            await StopExistingOllamaProcesses();

            bool modelLoaded = await CheckModelLoadedAsync();

            if (modelLoaded)
            {
                await Task.Run(() =>
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = _config.OllamaPath,
                        Arguments = "serve",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    };

                    psi.EnvironmentVariables["OLLAMA_MODELS"] = _config.OllamaModelsDir;

                    _ollamaProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };

                    _ollamaProcess.OutputDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                            Debug.WriteLine($"Ollama: {e.Data}");
                    };

                    _ollamaProcess.ErrorDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                            Debug.WriteLine($"Ollama: {e.Data}");
                    };

                    _ollamaProcess.Start();
                    _ollamaProcess.BeginOutputReadLine();
                    _ollamaProcess.BeginErrorReadLine();
                });
            }
            else
            {
                Debug.WriteLine("Model could not be loaded.");
            }
        }

        public void Stop()
        {
            if (_ollamaProcess != null && !_ollamaProcess.HasExited)
            {
                try
                {
                    _ollamaProcess.Kill(true);
                    _ollamaProcess.Dispose();
                }
                catch
                {
                    Debug.WriteLine("Error stopping Ollama");
                }
            }
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }

        private async Task<bool> CheckModelLoadedAsync()
        {
            string command = $"pull {_config.SelectedModel}";
            var outputBuilder = new System.Text.StringBuilder();

            await Task.Run(() =>
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _config.OllamaPath,
                    Arguments = command,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                psi.EnvironmentVariables["OLLAMA_MODELS"] = _config.OllamaModelsDir;
                Debug.WriteLine($"OLLAMA_MODELS set to: {_config.OllamaModelsDir}");

                using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

                process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        outputBuilder.AppendLine(e.Data);
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Debug.WriteLine($"Ollama: {e.Data}");
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            });

            string response = outputBuilder.ToString();
            return response.Contains("success", StringComparison.OrdinalIgnoreCase);
        }

        private async Task StopExistingOllamaProcesses()
        {
            try
            {
                var existingProcesses = Process.GetProcessesByName("ollama");
                foreach (var process in existingProcesses)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(5000); // Wait up to 5 seconds
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(
                            $"Failed to stop Ollama process {process.Id}: {ex.Message}"
                        );
                    }
                }

                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error stopping existing Ollama processes: {ex.Message}");
            }
        }
    }
}
