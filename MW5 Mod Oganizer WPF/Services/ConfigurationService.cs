﻿using Microsoft.Extensions.Configuration;
using System.IO;

namespace MW5_Mod_Organizer_WPF.Services
{
    public sealed class ConfigurationService
    {
        public IConfiguration config { get; set; }

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
    }
}
