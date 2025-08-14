using System;
using System.Diagnostics;
using System.Threading.Tasks;
using LibreOfficeAI.Services;
using LibreOfficeAI.ViewModels;
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
        private bool isPointerOver = false;

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
                ViewModel.RecordingStateChanged += UpdateMicrophoneButtonVisualState;

                // Add pointer event handlers for microphone button
                MicrophoneButton.PointerEntered += MicrophoneButton_PointerEntered;
                MicrophoneButton.PointerExited += MicrophoneButton_PointerExited;

                // Global keyboard event handler for spacebar
                this.KeyDown += MainPage_KeyDown;
                RootGrid.KeyDown += RootGrid_KeyDown;

                // Focus TextBox
                PromptTextBox.Loaded += (_, _) =>
                {
                    PromptTextBox.Focus(FocusState.Programmatic);
                };

                // Ensure the page can receive focus and gets focus when loaded
                this.Loaded += MainPage_Loaded;

                WelcomeBorder.SizeChanged += WelcomeBorder_SizeChanged;
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText("mainpage_exception.txt", ex.ToString());
                throw;
            }
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Make sure the page can receive focus
            this.IsTabStop = true;
            RootGrid.IsTabStop = true;

            // Set focus to the page itself to capture global keyboard events
            this.Focus(FocusState.Programmatic);

            Debug.WriteLine("MainPage loaded and focused");
        }

        private void MicrophoneButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            isPointerOver = true;
            UpdateMicrophoneButtonVisualState();
        }

        private void MicrophoneButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            isPointerOver = false;
            UpdateMicrophoneButtonVisualState();
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

        // Alternative handler on RootGrid
        private async void RootGrid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            Debug.WriteLine($"RootGrid KeyDown: {e.Key}");
            if (e.Key == Windows.System.VirtualKey.Space)
            {
                await HandleSpaceBarPress();
                e.Handled = true;
            }
        }

        // Spacebar used to trigger recording - Page level
        private async void MainPage_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            Debug.WriteLine($"MainPage KeyDown: {e.Key}");
            if (e.Key == Windows.System.VirtualKey.Space)
            {
                await HandleSpaceBarPress();
                e.Handled = true;
            }
        }

        private async Task HandleSpaceBarPress()
        {
            Debug.WriteLine("Spacebar pressed - HandleSpaceBarPress called");

            // Check if TextBox has focus - if so, let it handle the space normally
            var focusedElement = FocusManager.GetFocusedElement(this.XamlRoot);
            Debug.WriteLine($"Currently focused element: {focusedElement?.GetType().Name}");

            if (focusedElement == PromptTextBox)
            {
                Debug.WriteLine("TextBox has focus, not triggering microphone");
                return;
            }

            // Trigger microphone
            if (ViewModel.ToggleMicrophoneCommand.CanExecute(null))
            {
                Debug.WriteLine("Executing ToggleMicrophone command");
                await ViewModel.ToggleMicrophoneCommand.ExecuteAsync(null);
            }
            else
            {
                Debug.WriteLine("ToggleMicrophone command cannot execute");
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

        private void WelcomeBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            BackgroundRectangle.Width = WelcomeBorder.ActualWidth - 30; // 20px smaller
            BackgroundRectangle.Height = WelcomeBorder.ActualHeight - 30;
        }

        private void UpdateMicrophoneButtonVisualState()
        {
            string state;
            if (ViewModel.IsRecording)
            {
                state = isPointerOver ? "RecordingPointerOver" : "Recording";
            }
            else
            {
                // When not recording, explicitly go to NotRecording first to stop animations
                VisualStateManager.GoToState(MicrophoneButton, "NotRecording", true);
                state = isPointerOver ? "PointerOver" : "Normal";
            }
            VisualStateManager.GoToState(MicrophoneButton, state, true);
        }
    }
}
