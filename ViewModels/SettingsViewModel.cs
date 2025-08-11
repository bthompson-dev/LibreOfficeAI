using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibreOfficeAI.Services;

namespace LibreOfficeAI.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ConfigurationService _config;

        [ObservableProperty]
        private string? settingsChangedMessage = null;

        [ObservableProperty]
        private string documentsPath;

        [ObservableProperty]
        private string selectedModel;

        [ObservableProperty]
        private List<string> presentationTemplatesPaths;
        public string PresentationTemplatesPathsDisplay =>
            PresentationTemplatesPaths == null
                ? string.Empty
                : string.Join(Environment.NewLine, PresentationTemplatesPaths);

        public event Action? OnRequestNavigateToMainPage;

        public SettingsViewModel(ConfigurationService config)
        {
            _config = config;
            DocumentsPath = config.DocumentsPath;
            SelectedModel = config.SelectedModel;
            PresentationTemplatesPaths = config.PresentationTemplatesPaths;
        }

        [RelayCommand]
        private async Task Save()
        {
            // Check if nothing has changed
            if (
                DocumentsPath == _config.DocumentsPath
                && SelectedModel == _config.SelectedModel
                && PresentationTemplatesPaths.SequenceEqual(_config.PresentationTemplatesPaths)
            )
            {
                SettingsChangedMessage = "ℹ️ No changes made";
                return;
            }

            bool settingsSaved = await _config.SaveChangedSettings(
                DocumentsPath,
                SelectedModel,
                PresentationTemplatesPaths
            );

            if (settingsSaved)
            {
                SettingsChangedMessage = "✅ Changes saved";
            }
            else
            {
                SettingsChangedMessage = "❌ Error saving changes";
            }
        }

        [RelayCommand]
        private void BackButton_Click()
        {
            OnRequestNavigateToMainPage?.Invoke();
        }

        public void RefreshFromConfig()
        {
            DocumentsPath = _config.DocumentsPath;
            SelectedModel = _config.SelectedModel;
            PresentationTemplatesPaths = _config.PresentationTemplatesPaths;
        }
    }
}
