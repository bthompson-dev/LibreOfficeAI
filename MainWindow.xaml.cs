using LibreOfficeAI.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OllamaSharp;
using System;
using System.Collections.ObjectModel;
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

        private OllamaApiClient ollama;
        private Chat chat;

        public MainWindow()
        {
            InitializeComponent();

            // Attach an event handler to the KeyDown event of the PromptTextBox
            PromptTextBox.KeyDown += PromptTextBox_KeyDown;

            // Setting up Ollama
            var ollamaUri = new Uri("http://localhost:11434");
            ollama = new OllamaApiClient(ollamaUri);

            ollama.SelectedModel = "kitsonk/watt-tool-8B:latest";

            chat = new Chat(ollama);
        }

        private async void SendPrompt(string prompt)
        {
            var aiMessage = new ChatMessage { Text = "", IsUser = false };
            chatMessages.Add(aiMessage);

            // Scroll to bottom
            DispatcherQueue.TryEnqueue(() =>
            {
                ChatScrollViewer.ScrollToVerticalOffset(ChatScrollViewer.ScrollableHeight);
            });

            var stringBuilder = new StringBuilder();

            await foreach (var answerToken in chat.SendAsync(prompt))
            {
                stringBuilder.Append(answerToken);
                DispatcherQueue.TryEnqueue(() =>
                {
                    aiMessage.Text = stringBuilder.ToString();
                });

                await Task.Delay(10); // Slightly longer delay for better visual effect
            }
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

                chatMessages.Add(new ChatMessage { Text = userInput, IsUser = true });
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
    }
}
