using LibreOfficeAI.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OllamaSharp;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LibreOfficeAI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        // Dynamic collection of user messages - automatically updates
        public ObservableCollection<ChatMessage> chatMessages { get; } = new();

        private OllamaService ollamaService;
        private Chat chat;

        public bool aiTurn = true;

        public MainWindow()
        {
            InitializeComponent();

            // Attach an event handler to the KeyDown event of the PromptTextBox
            PromptTextBox.KeyDown += PromptTextBox_KeyDown;

            // Setting up Ollama
            ollamaService = new OllamaService();
            chat = ollamaService.Chat;

        }

        // Sending a prompt to the LLM model
        private async void SendPrompt(string prompt)
        {
            // Check that the service is running
            try
            {
                bool connected = await ollamaService.Client.IsRunningAsync();
                if (connected) Debug.WriteLine("Connected to Ollama");

            } catch (HttpRequestException ex)
            {
                Debug.WriteLine(ex.Message);
                return;
            }

            // Create a new AI message
            aiTurn = true;
            var aiMessage = new ChatMessage { Text = "", Type = MessageType.AI, IsLoading = true };
            chatMessages.Add(aiMessage);

            var stringBuilder = new StringBuilder();

            try
            {
                // Stream the AI response and update the message for each token
                await foreach (var answerToken in chat.SendAsync(prompt))
                {
                    stringBuilder.Append(answerToken);
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        aiMessage.Text = stringBuilder.ToString();
                    });

                    if (aiMessage.IsLoading) aiMessage.IsLoading = false;

                    // Scroll to the bottom                        
                    ChatScrollViewer.ScrollToVerticalOffset(ChatScrollViewer.ScrollableHeight);

                    await Task.Delay(10); // Slightly longer delay for better visual effect
                }
            }
            catch (HttpRequestException)
            {
                aiMessage.IsLoading = false;
                aiTurn = false;
                SendErrorMessage("Error connecting - please click to retry.");
            }

            aiTurn = false;
        }


        // When text is typed into the Prompt TextBox, the send button will appear
        private void PromptTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SendButton.Visibility = string.IsNullOrWhiteSpace(PromptTextBox.Text) ? Visibility.Collapsed : Visibility.Visible;
        }

        // Handles send button being clicked
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string userInput = PromptTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(userInput))
            {

                chatMessages.Add(new ChatMessage { Text = userInput, Type = MessageType.User });
                SendPrompt(userInput);
                PromptTextBox.Text = string.Empty;

                // Scroll to the last item
                DispatcherQueue.TryEnqueue(() =>
                {
                    ChatScrollViewer.ScrollToVerticalOffset(ChatScrollViewer.ScrollableHeight);
                });

            }
        }

        // Handles Enter being pressed to send a message
        private void PromptTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && !e.KeyStatus.IsMenuKeyDown)
            {
                SendButton_Click(sender, new RoutedEventArgs());
                e.Handled = true; // Prevents newline in TextBox
            }
        }

        private void SendErrorMessage(string message)
        {
            var errorMessage = new ChatMessage { Text = message, Type = MessageType.Error };
            chatMessages.Add(errorMessage);
        }
    }
}
