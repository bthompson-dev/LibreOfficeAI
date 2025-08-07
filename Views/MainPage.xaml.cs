using System;
using System.Diagnostics;
using LibreOfficeAI.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LibreOfficeAI.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel { get; }

        public MainPage()
            : this(App.Services) { }

        public MainPage(IServiceProvider serviceProvider)
        {
            try
            {
                InitializeComponent();

                // Get ViewModel from dependency injection container
                ViewModel = serviceProvider.GetRequiredService<MainViewModel>();

                // Set DataContext on the root Grid
                RootGrid.DataContext = ViewModel;

                // Subscribe to ViewModel events
                ViewModel.RequestScrollToBottom += OnRequestScrollToBottom;
                ViewModel.FocusTextBox += FocusTextBox;

                // Focus TextBox
                PromptTextBox.Loaded += (_, _) =>
                {
                    PromptTextBox.Focus(FocusState.Programmatic);
                };
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText("mainwindow_exception.txt", ex.ToString());
                throw;
            }
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

        // Double click to open files in use
        private void FilesListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (FilesListView.SelectedItem is Document doc)
            {
                try
                {
                    Process.Start(
                        new ProcessStartInfo { FileName = doc.Path, UseShellExecute = true }
                    );
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Could not open file: {ex.Message}");
                }
            }
        }
    }
}
