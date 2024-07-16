﻿using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Xaml.Behaviors.Media;
using MW5_Mod_Organizer_WPF.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MW5_Mod_Organizer_WPF.ViewModels
{
    public sealed partial class AboutViewModel : ObservableObject
    {
        private readonly ConfigurationService _configurationService;

        public string Title => _configurationService.AppTitle;
        public string Version => "Version " + _configurationService.Config!.GetValue<string>("Version");
        public string Copyright => "Copyright © Maxim Agemans";
        public string Distribution => "This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.";
        public string Source => "The source is not yet available.";
        public string License => "MW5 Mod Organizer does not have an Open Source License and falls under the default copyright laws. Therefor, it is not allowed to alter, sell or distribute this software without the permission of the author.";

        public AboutViewModel(ConfigurationService configurationService)
        { 
            _configurationService = configurationService;
        } 
    }
}
