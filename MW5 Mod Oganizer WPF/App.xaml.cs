using Microsoft.Extensions.DependencyInjection;
using MW5_Mod_Organizer_WPF.Services;
using MW5_Mod_Organizer_WPF.Facades;
using MW5_Mod_Organizer_WPF.ViewModels;
using System;
using System.Windows;

namespace MW5_Mod_Organizer_WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public sealed partial class App : Application
    {
        /// <summary>
        /// Gets the current <see cref="App"/> instance in use
        /// </summary>
        public new static App Current => (App)Application.Current;

        /// <summary>
        /// Get the <see cref="IServiceProvider"/> instance to resolve application services
        /// </summary>
        public IServiceProvider Services { get; private set; }

        public App()
        {
            Services = ConfigureServices();

            this.InitializeComponent();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            ModService.GetInstance();

            MainWindow = new MainWindow();
            MainWindow.Show();
            
            base.OnStartup(e);
        }

        /// <summary>
        /// Configures the services for the application
        /// </summary>
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Services
            services.AddTransient<FileHandlerService>();
            services.AddTransient<JsonHandlerService>();
            services.AddSingleton<ModService>();

            // Facades
            services.AddTransient<JsonConverterFacade>();

            // ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddTransient<ModViewModel>();

            return services.BuildServiceProvider();
        }
    }
}
