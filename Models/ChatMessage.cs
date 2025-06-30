using CommunityToolkit.Mvvm.ComponentModel;

namespace LibreOfficeAI.Models
{
    public partial class ChatMessage : ObservableObject
    {
        [ObservableProperty]
        private string text = string.Empty;
        public bool IsUser { get; set; }
    }
}
