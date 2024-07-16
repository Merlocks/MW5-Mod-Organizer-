using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Documents;

namespace MW5_Mod_Organizer_WPF.Services
{
    public sealed class ConfigurationService
    {
        public IConfiguration? Config { get; private set; }
        public IConfiguration? Credits { get; private set; }

        public string AppTitle => GetAppTitle();

        public ConfigurationService() 
        {
            try
            {
                var host = Assembly.GetEntryAssembly();

                if (host == null)
                    return;

                initializeAppsettings(host);
                initializeCredits(host);
            }
            catch (Exception e)
            {

                Console.WriteLine("Exception in ConfigurationService constructor." + e.Message);
            }


            //var builder = new ConfigurationBuilder()
            //    .SetBasePath(Directory.GetCurrentDirectory())
            //    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            //this.Config = builder.Build();

            // How to use:
            /*
             * Config.GetValue<string>("debugmode")
             */
        }

        public string GetAppTitle()
        {
            if (this.Config != null && this.Config.GetValue<string>("environment") == "dev")
            {
                return "MW5 Mod Organizer DEV BUILD";
            }
            else
            {
                return "MW5 Mod Organizer";
            }
        }

        private void initializeAppsettings(Assembly host)
        {
            string appsettings = host.GetManifestResourceNames().Single(str => str.EndsWith("appsettings.json"));

            using var input = host.GetManifestResourceStream(appsettings);
            if (input != null)
            {
                var builder = new ConfigurationBuilder()
                .AddJsonStream(input);

                this.Config = builder.Build();
            }
            else
            {
                Console.WriteLine(appsettings);
            }
        }

        private void initializeCredits(Assembly host)
        {
            string credits = host.GetManifestResourceNames().Single(str => str.EndsWith("credits.json"));

            using var input = host.GetManifestResourceStream(credits);
            if (input != null)
            {
                var builder = new ConfigurationBuilder()
                .AddJsonStream(input);

                this.Credits = builder.Build();
            }
            else
            {
                Console.WriteLine(credits);
            }
        }
    }
}
