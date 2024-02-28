using CommunityToolkit.Mvvm.ComponentModel;
using MW5_Mod_Organizer_WPF.Models;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel.DataAnnotations;
using MW5_Mod_Organizer_WPF.Services;
using System.Windows;

namespace MW5_Mod_Organizer_WPF.ViewModels
{
    public sealed partial class ProfilesViewModel : ObservableObject
    {
        private MainViewModel mainViewModel => App.Current.Services.GetService<MainViewModel>()!;
        private readonly ProfilesService profilesService;
        
        [ObservableProperty]
        private ObservableCollection<ProfileViewModel> profiles;

        [ObservableProperty]
        private string? textBoxContent;

        public ProfilesViewModel(ProfilesService profilesService)
        {
            this.profilesService = profilesService;
            
            profiles = new ObservableCollection<ProfileViewModel>();
        }

        [RelayCommand]
        public void SaveProfile()
        {
            if (!string.IsNullOrEmpty(this.TextBoxContent))
            {
                // DEBUG TIMER
                var timer = Stopwatch.StartNew();
                
                List<ModViewModel> mods = mainViewModel.ModVMCollection.ToList();
                ProfileContainer profileContainer = new ProfileContainer();
                Profile profile = new Profile(this.TextBoxContent);

                List<Task> tasks = new List<Task>();

                // Start task1 to retrieve all profiles
                // Add task to list of tasks
                Task task1 = Task.Run(() => 
                {
                    foreach (var item in this.Profiles)
                    {
                        profileContainer.Profiles.Add(item._profile.Name, item._profile);
                    }
                });

                tasks.Add(task1);

                // Start task2 to add all current mods to new profile
                // Add task to list of tasks
                Task task2 = Task.Run(() => 
                {
                    foreach (var item in mods.Where(m => m.IsEnabled))
                    {
                        profile.Entries.Add(item.DisplayName!, item.IsEnabled);
                    }
                });

                tasks.Add(task2);

                // Wait on completion of all tasks
                Task.WhenAll(tasks).Wait();

                // Add new profile to both list and container
                this.Profiles.Add(new ProfileViewModel(profile));
                profileContainer.Profiles.Add(profile.Name, profile);

                // Add logic for writing profileContainer to file
                this.profilesService.SaveProfiles(profileContainer);

                // Clear textBoxContent
                this.TextBoxContent = string.Empty;

                // DEBUG TIMER
                timer.Stop();
                var time = timer.ElapsedMilliseconds;

                Console.WriteLine($"SaveProfile elapsed debug time: {time}ms");
            }
        }

        [RelayCommand]
        public async Task WindowLoadedAsync(RoutedEventArgs e)
        {
            ProfileContainer profileContainer =  await this.profilesService.GetProfilesAsync();

            foreach (var item in profileContainer.Profiles)
            {
                this.Profiles.Add(new ProfileViewModel(item.Value));
            }

            e.Handled = true;
        }
    }
}


