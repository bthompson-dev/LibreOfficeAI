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

        public MainWindow(IServiceProvider serviceProvider)
        {
            try
            {
                InitializeComponent();
                MainFrame.Navigate(typeof(LibreOfficeAI.Views.MainPage));

                // Get ViewModel from dependency injection container
                ViewModel = serviceProvider.GetRequiredService<MainViewModel>();

                ViewModel.OnRequestNavigateToSettings += () =>
                {
                    MainFrame.Navigate(typeof(LibreOfficeAI.Views.SettingsPage));
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
