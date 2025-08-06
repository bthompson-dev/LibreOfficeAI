using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;

namespace LibreOfficeAI.Models
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly OllamaService ollamaService;
        private readonly DispatcherQueue dispatcherQueue;
        private readonly DocumentService documentService;
        private readonly ConfigurationService config;

        [ObservableProperty]
        private bool aiTurn = false;

        [ObservableProperty]
        private string promptText = string.Empty;

        [ObservableProperty]
        private bool isSendButtonVisible = false;

        // Variables for Ollama loading screen
        public bool OllamaReady => ollamaService.OllamaReady;
        public string OllamaStatus => ollamaService.OllamaStatus;
        public int ModelPercentage => ollamaService.ModelPercentage;

        // Documents to display in UI
        public ObservableCollection<Document> DocumentsInUse => documentService.DocumentsInUse;

        // Cancellation token
        CancellationTokenSource cts = new();

        // Dynamic collection of user messages - automatically updates
        public ObservableCollection<ChatMessage> ChatMessages { get; set; } = [];

        // Events linked to Main Window
        public event Action? RequestScrollToBottom;
        public event Action? FocusTextBox;

        public MainViewModel(
            OllamaService ollamaService,
            DocumentService documentService,
            Func<DispatcherQueue> dispatcherQueueFactory,
            ConfigurationService config
        )
        {
            this.ollamaService = ollamaService;
            this.documentService = documentService;
            this.dispatcherQueue = dispatcherQueueFactory();
            this.config = config;

            this.ollamaService.PropertyChanged += OnOllamaServicePropertyChanged;

            SetupToolEventHandlers();
        }

        private void OnOllamaServicePropertyChanged(
            object? sender,
            System.ComponentModel.PropertyChangedEventArgs e
        )
        {
            if (e.PropertyName == nameof(OllamaService.OllamaReady))
            {
                Debug.WriteLine(
                    "MainViewModel: OllamaReady property changed notification received"
                );
                // Notify the UI that OllamaReady has changed
                dispatcherQueue.TryEnqueue(() =>
                {
                    OnPropertyChanged(nameof(OllamaReady));
                });
            }

            if (e.PropertyName == nameof(OllamaService.OllamaStatus))
            {
                // Notify the UI that OllamaStatus has changed
                dispatcherQueue.TryEnqueue(() =>
                {
                    OnPropertyChanged(nameof(OllamaStatus));
                });
            }

            if (e.PropertyName == nameof(OllamaService.ModelPercentage))
            {
                // Notify the UI that OllamaStatus has changed
                dispatcherQueue.TryEnqueue(() =>
                {
                    OnPropertyChanged(nameof(ModelPercentage));
                });
            }
        }

        // Sets send button visibility if text is present
        partial void OnPromptTextChanged(string value)
        {
            IsSendButtonVisible = !string.IsNullOrWhiteSpace(value);
            SendMessageCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(CanSendMessage))]
        private async Task SendMessageAsync()
        {
            string userInput = PromptText.Trim();
            if (string.IsNullOrEmpty(userInput))
                return;

            // Add user message
            ChatMessages.Add(new ChatMessage { Text = userInput, Type = MessageType.User });

            // Clear input
            PromptText = string.Empty;

            // Request scroll to bottom
            RequestScrollToBottom?.Invoke();

            // Send to AI
            await SendPromptAsync(userInput);
        }

        // Can only send a message once AI is ready, if it is not the AI's turn, and the prompt is not empty
        private bool CanSendMessage() =>
            !AiTurn
            && !string.IsNullOrWhiteSpace(PromptText)
            && ollamaService.ToolService.ToolsLoaded;

        // Sending a prompt to the LLM model
        private async Task SendPromptAsync(string prompt)
        {
            // Check that the service is running
            try
            {
                bool connected = await ollamaService.Client.IsRunningAsync();
                if (connected)
                    Debug.WriteLine("Ollama Client running");
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine(ex.Message);
                SendErrorMessage("Could not find the AI service.");
                return;
            }

            // Create a new AI message
            AiTurn = true;
            var aiMessage = new ChatMessage
            {
                Text = "",
                Type = MessageType.AI,
                IsLoading = true,
                IsThinking = false,
            };
            ChatMessages.Add(aiMessage);

            // Wait for UI to update
            await Task.Delay(2);
            RequestScrollToBottom?.Invoke();

            var response = new StringBuilder();
            var fullResponse = new StringBuilder();

            try
            {
                // First run the prompt on the internal chat, to find useful tools
                var toolsToCall = await ollamaService.ToolService.FindNeededTools(prompt);

                // Log all suggested tools
                if (toolsToCall != null)
                {
                    foreach (var tool in toolsToCall)
                    {
                        Debug.WriteLine(tool?.Function?.Name);
                    }
                }

                // If any documents are in use, add the file paths to the user prompt
                string documentsInUseString = documentService.GetDocumentsInUseString();
                if (!string.IsNullOrEmpty(documentsInUseString))
                {
                    prompt += $" Current documents: {documentsInUseString}.";
                }

                // Stream the AI response and update the message for each token
                await foreach (
                    var answerToken in ollamaService.ExternalChat.SendAsync(
                        prompt,
                        toolsToCall,
                        null, // imagesAsBase64
                        null, // format
                        cts.Token
                    )
                )
                {
                    fullResponse.Append(answerToken);
                    aiMessage.IsLoading = false;

                    // Check if the model has started thinking
                    if (answerToken == "<think>")
                    {
                        aiMessage.IsThinking = true;
                    }

                    // Once the model has stopped thinking, print tokens
                    if (!aiMessage.IsThinking)
                    {
                        // Prevent newlines being added after thinking
                        if (response.Length != 0 || !string.IsNullOrWhiteSpace(answerToken))
                        {
                            response.Append(answerToken);
                            dispatcherQueue.TryEnqueue(() =>
                            {
                                aiMessage.Text = response.ToString();

                                // Scroll to the bottom
                                RequestScrollToBottom?.Invoke();
                            });
                        }

                        await Task.Delay(10); // Slightly longer delay for better visual effect
                    }

                    // If the model has stopped thinking, we can show the next token
                    if (answerToken == "</think>")
                    {
                        aiMessage.IsThinking = false;
                    }
                }
            }
            catch (HttpRequestException)
            {
                aiMessage.IsLoading = false;
                AiTurn = false;
                SendErrorMessage("Error connecting - please click to retry.");
            }

            Debug.WriteLine(fullResponse);
            AiTurn = false;
            SendMessageCommand.NotifyCanExecuteChanged();
            FocusTextBox?.Invoke();
        }

        [RelayCommand]
        private void CancelPrompt()
        {
            // Use the cancellation token service to cancel the request
            cts.Cancel();

            // Remove the last chat message (AI)
            ChatMessages.RemoveAt(ChatMessages.Count - 1);

            // Enable the chat input
            AiTurn = false;
            SendMessageCommand.NotifyCanExecuteChanged();

            // Reset the cancellation token
            cts = new CancellationTokenSource();
        }

        [RelayCommand]
        private void NewChat()
        {
            ollamaService.RefreshChat();
            ChatMessages.Clear();
            SetupToolEventHandlers();
        }

        private void SendErrorMessage(string message)
        {
            var errorMessage = new ChatMessage { Text = message, Type = MessageType.Error };
            ChatMessages.Add(errorMessage);
        }

        public void RegisterToolCall(string tool)
        {
            dispatcherQueue.TryEnqueue(() =>
            {
                var currentMessage = ChatMessages[^1];
                currentMessage.ToolCalls.Add(tool);
                Debug.WriteLine(tool);
            });
        }

        private void SetupToolEventHandlers()
        {
            // Add event listener for tool calls
            ollamaService.ExternalChat.OnToolCall += (sender, toolCall) =>
            {
                if (toolCall.Function?.Name != null)
                {
                    Debug.WriteLine($"Tool called: {toolCall.Function.Name}");
                    RegisterToolCall(toolCall.Function.Name);

                    foreach (var argument in toolCall.Function.Arguments)
                    {
                        Debug.WriteLine(argument.ToString());
                    }
                }
            };

            // Add event listener for tool results
            ollamaService.ExternalChat.OnToolResult += (sender, result) =>
            {
                Debug.WriteLine($"Tool result: {result.Result}");

                var arguments = result.ToolCall.Function?.Arguments;
                var function = result.ToolCall.Function?.Name;

                // Add any documents used to the list of current documents
                if (arguments != null)
                {
                    foreach (var key in new[] { "file_path", "source_path", "target_path" })
                    {
                        if (arguments.TryGetValue(key, out var value))
                        {
                            // value is returned as an object
                            var filePath = value as string ?? value?.ToString();
                            if (!string.IsNullOrEmpty(filePath))
                            {
                                Debug.WriteLine($"Adding file to docs in use: {filePath}");
                                dispatcherQueue.TryEnqueue(() =>
                                {
                                    documentService.AddDocumentInUse(filePath);
                                });
                            }
                        }
                    }

                    // Specific to create_blank_document and create_blank_presentation functions
                    if (arguments.TryGetValue("filename", out var fileNameObj))
                    {
                        // value is returned as an object - cast to string
                        string? fileName = fileNameObj as string ?? fileNameObj?.ToString();
                        if (string.IsNullOrEmpty(fileName))
                            return;

                        // Add file extension if not included in the fileName
                        if (function == "create_blank_document")
                        {
                            string? fileExtension = documentService.writerExtensions.FirstOrDefault(
                                ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)
                            );

                            if (string.IsNullOrEmpty(fileExtension))
                            {
                                fileExtension = ".odt";
                                fileName += fileExtension;
                            }
                        }

                        if (function == "create_blank_presentation")
                        {
                            string? fileExtension =
                                documentService.impressExtensions.FirstOrDefault(ext =>
                                    fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)
                                );

                            if (string.IsNullOrEmpty(fileExtension))
                            {
                                fileExtension = ".odp";
                                fileName += fileExtension;
                            }
                        }

                        // Add the base documents path
                        string filePath = $"{config.DocumentsPath}\\{fileName}";

                        Debug.WriteLine($"Adding file to docs in use: {filePath}");
                        dispatcherQueue.TryEnqueue(() =>
                        {
                            documentService.AddDocumentInUse(filePath);
                        });
                    }
                }
            };
        }
    }
}
