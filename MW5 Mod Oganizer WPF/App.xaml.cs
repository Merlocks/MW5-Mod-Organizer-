using Microsoft.Extensions.DependencyInjection;
using MW5_Mod_Organizer_WPF.Services;
using MW5_Mod_Organizer_WPF.Facades;
using MW5_Mod_Organizer_WPF.ViewModels;
using MW5_Mod_Organizer_WPF.Properties;
using System;
using System.Windows;
using System.Threading.Tasks;

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
            _modService;

            MainWindow = new MainWindow();
            MainWindow.Show();

            // Resets all user scoped application settings when user saved version string doesn't match the default
            // Allows for checking if application has been run before on a different version
            if (Settings.Default.Version != Settings.Default.Properties["Version"].DefaultValue.ToString())
            {
                Settings.Default.Reset();
                Settings.Default.Save();
            }

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
            services.AddSingleton<HttpRequestService>();

            // Facades
            services.AddTransient<JsonConverterFacade>();

            // ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddTransient<ModViewModel>();

            return services.BuildServiceProvider();
        }
    }
}
