using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LibreOfficeAI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        // Dynamic collection of user messages - automatically updates
        private ObservableCollection<string> chatMessages = new();

        public MainWindow()
        {
            InitializeComponent();
            ChatListBox.ItemsSource = chatMessages;

            // Attach an event handler to the KeyDown event of the PromptTextBox
            PromptTextBox.KeyDown += PromptTextBox_KeyDown;
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
                chatMessages.Add(userInput);
                PromptTextBox.Text = string.Empty;

                // Scroll to the last item
                if (ChatListBox.Items.Count > 0)
                {
                    ChatListBox.ScrollIntoView(ChatListBox.Items[^1]);
                }

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
