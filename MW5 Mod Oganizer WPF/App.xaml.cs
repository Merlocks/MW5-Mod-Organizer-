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
            Console.WriteLine($"If you see this you are running a beta version of MW5 Mod Organizer.");
            Console.WriteLine("");
            Console.WriteLine($"This is the debug console. If any Exceptions appear on the console,");
            Console.WriteLine($"please report them to @Merlock in the Yet Another MW5 Discord Server");
            Console.WriteLine($"--------------------------------------------------------------------");
            Console.WriteLine("");

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
            services.AddTransient<ProfilesService>();
            services.AddSingleton<IModService, ModService>();
            services.AddSingleton<HttpRequestService>();
            services.AddSingleton<ConfigurationService>();

            // Facades
            services.AddTransient<JsonConverterFacade>();

            // ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddTransient<ModViewModel>();
            services.AddTransient<ProfilesViewModel>();
            services.AddTransient<ExportProfilesViewModel>();
            services.AddTransient<AboutViewModel>();
            services.AddTransient<SettingsViewModel>();

            return services.BuildServiceProvider();
        }
    }
}
