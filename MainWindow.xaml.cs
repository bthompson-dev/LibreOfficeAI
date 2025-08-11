using System;
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

        public MainWindow(IServiceProvider serviceProvider)
        {
            try
            {
                InitializeComponent();
                MainFrame.Navigate(typeof(LibreOfficeAI.Views.MainPage));

                // Get ViewModel from dependency injection container
                ViewModel = serviceProvider.GetRequiredService<MainViewModel>();
                SettingsVM = serviceProvider.GetRequiredService<SettingsViewModel>();

                ViewModel.OnRequestNavigateToSettings += () =>
                {
                    SettingsVM.RefreshFromConfig();
                    MainFrame.Navigate(typeof(LibreOfficeAI.Views.SettingsPage));
                };

                SettingsVM.OnRequestNavigateToMainPage += () =>
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
