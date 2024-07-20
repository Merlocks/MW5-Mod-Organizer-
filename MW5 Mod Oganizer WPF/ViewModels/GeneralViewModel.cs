using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MW5_Mod_Organizer_WPF.ViewModels
{
    public sealed partial class GeneralViewModel : ObservableObject
    {
        private MainViewModel? _mainViewModel => App.Current.Services.GetService<MainViewModel>();
        
        [ObservableProperty]
        private string? gameVersion;

        partial void OnGameVersionChanged(string? oldValue, string? newValue)
        {
            if (newValue == Properties.Settings.Default.GameVersion) 
                return;
            
            Properties.Settings.Default.GameVersion = newValue;
            Properties.Settings.Default.Save();

            if (_mainViewModel != null)
            {
                _mainViewModel.DeploymentNecessary = true;
            }
        }

        public GeneralViewModel() 
        { 
            this.GameVersion = Properties.Settings.Default.GameVersion;
        }
    }
}
