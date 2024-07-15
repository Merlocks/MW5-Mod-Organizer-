using Microsoft.Extensions.Configuration;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MW5_Mod_Organizer_WPF.Services
{
    public sealed class ConfigurationService
    {
        public IConfiguration? Config { get; private set; }

        public string AppTitle => GetAppTitle();

        public ConfigurationService() 
        {
            try
            {
                var host = Assembly.GetEntryAssembly();

                if (host == null)
                    return;

                string resourceName = host.GetManifestResourceNames().Single(str => str.EndsWith("appsettings.json"));

                using var input = host.GetManifestResourceStream(resourceName);
                if (input != null)
                {
                    var builder = new ConfigurationBuilder()
                    .AddJsonStream(input);

                    this.Config = builder.Build();
                }
                else
                {
                    Console.WriteLine(resourceName);
                }
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
    }
}
