using CommunityToolkit.Mvvm.ComponentModel;
using MW5_Mod_Organizer_WPF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MW5_Mod_Organizer_WPF.ViewModels
{
    public partial class ProfileViewModel : ObservableObject
    {
        public Profile _profile;

        [ObservableProperty]
        private string name;

        partial void OnNameChanged(string value)
        {
            _profile.Name = value;
        }

        public ProfileViewModel(Profile profile)
        {
            _profile = profile;
            this.Name = _profile.Name;
        }
    }
}
