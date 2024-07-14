using Microsoft.Extensions.Configuration;
using System.IO;

namespace MW5_Mod_Organizer_WPF.Services
{
    public sealed class ConfigurationService
    {
        public IConfiguration config { get; private set; }

        public ConfigurationService() 
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true); 

            this.config = builder.Build();

            // How to use:
            /*
             * config.GetValue<string>("debugmode")
             */
        }

        public string GetAppTitle()
        {
            if (this.config.GetValue<string>("debugmode") == "ON")
            {
                return "MW5 Mod Organizer DEBUG VERSION";
            } 
            else
            {
                return "MW5 Mod Organizer";
            }
        }
    }
}
