using System;
using System.Collections;
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
using OllamaSharp;

namespace LibreOfficeAI.Models
{
    public partial class MainViewModel(OllamaService ollamaService, DispatcherQueue dispatcherQueue)
        : ObservableObject
    {
        private readonly OllamaService ollamaService = ollamaService;
        private readonly DispatcherQueue dispatcherQueue = dispatcherQueue;

        [ObservableProperty]
        private bool aiTurn = false;

        [ObservableProperty]
        private string promptText = string.Empty;

        [ObservableProperty]
        private bool isSendButtonVisible = false;

        // Cancellation token
        CancellationTokenSource cts = new();

        // Dynamic collection of user messages - automatically updates
        public ObservableCollection<ChatMessage> ChatMessages { get; set; } = [];

        // Events linked to Main Window
        public event Action? RequestScrollToBottom;
        public event Action? FocusTextBox;

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
                    Debug.WriteLine("Ollama running");
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
            };
            ChatMessages.Add(aiMessage);

            // Waits for UI to update
            await Task.Delay(2);
            RequestScrollToBottom?.Invoke();

            var toolsToCall = await ollamaService.FindNeededTools(prompt);

            var response = new StringBuilder();

            try
            {
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
                    response.Append(answerToken);
                    dispatcherQueue.TryEnqueue(() =>
                    {
                        aiMessage.Text = response.ToString();
                        if (aiMessage.IsLoading)
                            aiMessage.IsLoading = false;

                        // Scroll to the bottom
                        RequestScrollToBottom?.Invoke();
                    });

                    await Task.Delay(10); // Slightly longer delay for better visual effect
                }
            }
            catch (HttpRequestException)
            {
                aiMessage.IsLoading = false;
                AiTurn = false;
                SendErrorMessage("Error connecting - please click to retry.");
            }

            AiTurn = false;
            SendMessageCommand.NotifyCanExecuteChanged();
            FocusTextBox?.Invoke();

            // Look for any tool calls
            var lastMessage = ollamaService.ExternalChat.Messages.Last();
            Debug.WriteLine(lastMessage.Content);
            if (lastMessage.ToolCalls?.Any() == true)
            {
                foreach (var toolCall in lastMessage.ToolCalls)
                {
                    Debug.WriteLine($"Tool called: {toolCall.Function?.Name}");
                    Debug.WriteLine(
                        $"Arguments: {string.Join(", ", toolCall.Function?.Arguments?.Select(kvp => $"{kvp.Key}: {kvp.Value}") ?? [])}"
                    );
                }
            }
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
            ollamaService.RefreshExternalChat();
            ChatMessages.Clear();
        }

        private void SendErrorMessage(string message)
        {
            var errorMessage = new ChatMessage { Text = message, Type = MessageType.Error };
            ChatMessages.Add(errorMessage);
        }
    }
}
