using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using OllamaSharp;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LibreOfficeAI.Models
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly OllamaService ollamaService;
        private readonly Chat chat;
        private readonly DispatcherQueue dispatcherQueue;

        [ObservableProperty]
        private bool aiTurn = false;

        [ObservableProperty]
        private string promptText = string.Empty;

        [ObservableProperty]
        private bool isSendButtonVisible = false;

        // Cancellation token
        CancellationTokenSource cts = new();

        // Dynamic collection of user messages - automatically updates
        public ObservableCollection<ChatMessage> ChatMessages { get; } = new();

        // Event to request scrolling to bottom
        public event Action? RequestScrollToBottom;

        // Constructor - initialised with the ollamaService and DispatcherQueue
        public MainViewModel(OllamaService ollamaService, DispatcherQueue dispatcherQueue)
        {
            this.ollamaService = ollamaService;
            this.chat = ollamaService.Chat;
            this.dispatcherQueue = dispatcherQueue;
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
            if (string.IsNullOrEmpty(userInput)) return;

            // Add user message
            ChatMessages.Add(new ChatMessage { Text =  userInput, Type = MessageType.User });

            // Clear input
            PromptText = string.Empty;

            // Request scroll to bottom
            RequestScrollToBottom?.Invoke();

            // Send to AI
            await SendPromptAsync(userInput);

        }

        // Can only send a message if it is not the AI's turn, and the prompt is not empty
        private bool CanSendMessage() => !AiTurn && !string.IsNullOrWhiteSpace(PromptText);


        // Sending a prompt to the LLM model
        private async Task SendPromptAsync(string prompt)
        {
            // Check that the service is running
            try
            {
                bool connected = await ollamaService.Client.IsRunningAsync();
                if (connected) Debug.WriteLine("Connected to Ollama");

            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine(ex.Message);
                return;
            }

            // Create a new AI message
            AiTurn = true;
            var aiMessage = new ChatMessage { Text = "", Type = MessageType.AI, IsLoading = true };
            ChatMessages.Add(aiMessage);

            var stringBuilder = new StringBuilder();

            try
            {
                // Stream the AI response and update the message for each token
                await foreach (var answerToken in chat.SendAsync(prompt, cts.Token))
                {
                    stringBuilder.Append(answerToken);
                    dispatcherQueue.TryEnqueue(() =>
                    {
                        aiMessage.Text = stringBuilder.ToString();
                        if (aiMessage.IsLoading) aiMessage.IsLoading = false;

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
        }

        private void SendErrorMessage(string message)
        {
            var errorMessage = new ChatMessage { Text = message, Type = MessageType.Error };
            ChatMessages.Add(errorMessage);
        }

    }

}
