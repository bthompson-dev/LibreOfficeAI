using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace LibreOfficeAI.Models
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool aiTurn = false;

        // Dynamic collection of user messages - automatically updates
        public ObservableCollection<ChatMessage> chatMessages { get; } = new();
    }

}
