using CommunityToolkit.Mvvm.ComponentModel;
using MW5_Mod_Organizer_WPF.Models;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace MW5_Mod_Organizer_WPF.ViewModels
{
    public partial class ProfilesViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ProfileViewModel> profiles;
        
        public ProfilesViewModel()
        {
            profiles = new ObservableCollection<ProfileViewModel>();

            PopulateProfilesCollection();
        }

        private void PopulateProfilesCollection()
        {
            
            Profile profile1 = new Profile("Profile1");
            profile1.Entries.Add("mod", true);

            Profile profile2 = new Profile("Profile2");
            profile2.Entries.Add("mod", true);

            Profile profile3 = new Profile("Profile3");
            profile3.Entries.Add("mod", true);

            ProfileContainer profileContainer = new ProfileContainer();
            profileContainer.Profiles.Add(profile1.Name, profile1);
            profileContainer.Profiles.Add(profile2.Name, profile2);
            profileContainer.Profiles.Add(profile3.Name, profile3);

            foreach (var item in profileContainer.Profiles)
            {
                ProfileViewModel profileViewModel = new ProfileViewModel(item.Value);
                this.Profiles.Add(profileViewModel);
            }
        }
    }
}


