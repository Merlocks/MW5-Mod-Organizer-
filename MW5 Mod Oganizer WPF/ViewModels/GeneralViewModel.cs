using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MW5_Mod_Organizer_WPF.ViewModels
{
    public sealed partial class GeneralViewModel : ObservableObject
    {

        [ObservableProperty]
        private string? gameVersion;

        partial void OnGameVersionChanging(string? value)
        {
            Properties.Settings.Default.GameVersion = value;
            Properties.Settings.Default.Save();

            if (!string.IsNullOrEmpty(Properties.Settings.Default.Path))
            {
                //TODO set DeploymentNecessary in MainViewModel to true
            }
        }
        public GeneralViewModel() 
        { 
            this.GameVersion = Properties.Settings.Default.GameVersion;
        }
    }
}
