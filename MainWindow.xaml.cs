using LibreOfficeAI.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace LibreOfficeAI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            InitializeComponent();

            // Setting up Ollama
            var ollamaService = new OllamaService();

            // Create ViewModel with dependencies
            ViewModel = new MainViewModel(ollamaService, this.DispatcherQueue);

            // Set DataContext on the root Grid
            RootGrid.DataContext = ViewModel;

            // Subscribe to ViewModel events
            ViewModel.RequestScrollToBottom += OnRequestScrollToBottom;
            ViewModel.FocusTextBox += FocusTextBox;

            // Focus TextBox
            PromptTextBox.Loaded += (object sender, RoutedEventArgs args) =>
            {
                PromptTextBox.Focus(FocusState.Programmatic);
            };

        }

        private void FocusTextBox()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                PromptTextBox.Focus(FocusState.Programmatic);
            });
        }

        private void OnRequestScrollToBottom()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                ChatScrollViewer.ScrollToVerticalOffset(ChatScrollViewer.ScrollableHeight);
            });
        }

        // Handles Enter being pressed to send a message
        private void PromptTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && !e.KeyStatus.IsMenuKeyDown)
            {
                if (ViewModel.SendMessageCommand.CanExecute(null))
                {
                    _ = ViewModel.SendMessageCommand.ExecuteAsync(null);
                }
                e.Handled = true; // Prevents newline in TextBox
            }
        }
    }
}
