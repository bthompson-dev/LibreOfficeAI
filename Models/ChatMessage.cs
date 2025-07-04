using CommunityToolkit.Mvvm.ComponentModel;

namespace LibreOfficeAI.Models
{
    public partial class ChatMessage : ObservableObject
    {
        [ObservableProperty]
        private string text = string.Empty;

        [ObservableProperty]
        private bool isLoading = false;
        public MessageType Type { get; set; }
    }
}

public enum MessageType
{
    User,
    AI,
    Error,
}
