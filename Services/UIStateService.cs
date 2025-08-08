using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LibreOfficeAI.Models
{
    public partial class UIStateService : ObservableObject
    {
        [ObservableProperty]
        private string _promptText = string.Empty;

        [ObservableProperty]
        private bool _isSendButtonVisible = false;

        // Events for UI interactions
        public event Action? FocusTextBox;

        partial void OnPromptTextChanged(string value)
        {
            IsSendButtonVisible = !string.IsNullOrWhiteSpace(value);
        }

        public void ClearPrompt()
        {
            PromptText = string.Empty;
        }

        public void RequestFocus()
        {
            FocusTextBox?.Invoke();
        }
    }
}
