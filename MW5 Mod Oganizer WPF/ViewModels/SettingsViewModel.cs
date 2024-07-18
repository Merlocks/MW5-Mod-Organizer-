using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace MW5_Mod_Organizer_WPF.ViewModels
{
    public sealed partial class SettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool bGeneralSettings;

        partial void OnBGeneralSettingsChanged(bool value)
        {
            Console.WriteLine($"General Settings status: {value}");
        }

        [ObservableProperty]
        private bool bAccessibilitySettings;

        partial void OnBAccessibilitySettingsChanged(bool value)
        {
            Console.WriteLine($"Accessibility Settings status: {value}");
        }


        public SettingsViewModel() 
        {
            Console.WriteLine("Viewmodel created");
        }

        [RelayCommand]
        public void SwitchSettings()
        {

        }
    }
}
