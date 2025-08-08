using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibreOfficeAI.Services;

namespace LibreOfficeAI.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ConfigurationService _config;

        [ObservableProperty]
        private string documentsPath;

        public SettingsViewModel(ConfigurationService config)
        {
            _config = config;
            documentsPath = config.DocumentsPath;
        }

        [RelayCommand]
        private void Save()
        {
            //_config.DocumentsPath = DocumentsPath;
            // Save to disk if needed
        }
    }
}
