using System;
using LibreOfficeAI.Services;
using LibreOfficeAI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace LibreOfficeAI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }
        public SettingsViewModel SettingsVM { get; }
        public HelpViewModel HelpVM { get; }

        public MainWindow(IServiceProvider serviceProvider, OllamaService ollamaService)
        {
            try
            {
                InitializeComponent();
                MainFrame.Navigate(typeof(LibreOfficeAI.Views.MainPage));

                // Get ViewModel from dependency injection container
                ViewModel = serviceProvider.GetRequiredService<MainViewModel>();
                SettingsVM = serviceProvider.GetRequiredService<SettingsViewModel>();
                HelpVM = serviceProvider.GetRequiredService<HelpViewModel>();

                ViewModel.OnRequestNavigateToSettings += () =>
                {
                    SettingsVM.RefreshFromConfig();
                    MainFrame.Navigate(typeof(LibreOfficeAI.Views.SettingsPage));
                };

                ViewModel.OnRequestNavigateToHelp += () =>
                {
                    MainFrame.Navigate(typeof(LibreOfficeAI.Views.HelpPage));
                };

                SettingsVM.OnRequestNavigateToMainPage += () =>
                {
                    if (SettingsVM.SelectedModelChanged)
                    {
                        ollamaService.RefreshAsync();
                    }
                    MainFrame.Navigate(typeof(LibreOfficeAI.Views.MainPage));
                };

                HelpVM.OnRequestNavigateToMainPage += () =>
                {
                    MainFrame.Navigate(typeof(LibreOfficeAI.Views.MainPage));
                };
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText("mainwindow_exception.txt", ex.ToString());
                throw;
            }
        }
    }
}
