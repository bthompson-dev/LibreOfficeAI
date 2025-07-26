using System;
using System.IO;
using LibreOfficeAI.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LibreOfficeAI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;
        private readonly IHost _host;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.UnhandledException += App_UnhandledException;
            InitializeComponent();

            // Configure dependency injection
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices(
                    (context, services) =>
                    {
                        // Configuration
                        services.AddSingleton<ConfigurationService>();

                        // Core services
                        services.AddSingleton<DocumentService>();
                        services.AddSingleton<OllamaService>();

                        // DispatcherQueue factory
                        services.AddSingleton<Func<DispatcherQueue>>(provider =>
                            () =>
                            {
                                return DispatcherQueue.GetForCurrentThread();
                            }
                        );

                        // ViewModels
                        services.AddTransient<MainViewModel>();
                    }
                )
                .Build();
        }

        private void App_UnhandledException(
            object sender,
            Microsoft.UI.Xaml.UnhandledExceptionEventArgs e
        )
        {
            try
            {
                File.WriteAllText("unhandled_exception.txt", e.Exception.ToString());
            }
            catch { }
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow(_host.Services);
            _window.Activate();
        }

        //Expose service provider for other components
        public static IServiceProvider Services => ((App)Current)._host.Services;
    }
}
