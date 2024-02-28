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
using System.Runtime.InteropServices;
using System.ComponentModel;
using MW5_Mod_Organizer_WPF.Subclasses;

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

                try
                {
                    List<ModViewModel> mods = mainViewModel.ModVMCollection.ToList();
                    Profile profile = new Profile(this.TextBoxContent);

                    Task task = Task.Run(() =>
                    {
                        foreach (var item in mods)
                        {
                            profile.Entries.Add(item.FolderName!, new ProfileEntryStatus() { IsEnabled = item.IsEnabled, LoadOrder = item.LoadOrder } );
                        }
                    });

                    // Wait on completion of all tasks.
                    task.Wait();

                    // Add new profile to both list.
                    this.Profiles.Add(new ProfileViewModel(profile));

                    // Clear textBoxContent.
                    this.TextBoxContent = string.Empty;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"-- ProfilesViewModel.SaveProfile -- {e.Message}");
                }

                // DEBUG TIMER
                timer.Stop();
                var time = timer.ElapsedMilliseconds;

                Console.WriteLine($"SaveProfile elapsed debug time: {time}ms");
            }
        }

        [RelayCommand]
        public void DeleteProfile()
        {
            try
            {
                ProfileViewModel? selectedProfile = Profiles.Where(p => p.IsSelected).FirstOrDefault();

                if (selectedProfile != null)
                {
                    Profiles.Remove(selectedProfile);
                }

            } catch (Exception e)
            {

                Console.WriteLine($"-- ProfilesViewModel.SaveProfile -- {e.Message}");
            }
        }

        [RelayCommand]
        public void ActivateProfile()
        {
            try
            {
                ProfileViewModel? selectedProfile = Profiles.Where(p => p.IsSelected).FirstOrDefault();
                RaisableObservableCollection<ModViewModel> collection = this.mainViewModel.ModVMCollection;

                if (selectedProfile != null)
                {
                    foreach (var item in CollectionsMarshal.AsSpan(collection.ToList()))
                    {
                        if (item.FolderName != null && selectedProfile._profile.Entries.ContainsKey(item.FolderName))
                        {
                            item.IsEnabled = selectedProfile._profile.Entries.GetValueOrDefault(item.FolderName)!.IsEnabled;
                            item.LoadOrder = selectedProfile._profile.Entries.GetValueOrDefault(item.FolderName)!.LoadOrder;
                        }
                        else
                        {
                            item.IsEnabled = false;
                        }
                    }

                    this.mainViewModel.ModVMCollection = new RaisableObservableCollection<ModViewModel>(collection.OrderBy(m => m.LoadOrder).ThenBy(m => m.FolderName));
                }

            } catch (Exception e)
            {
                Console.WriteLine($"-- ProfilesViewModel.ActivateProfile -- {e.Message}");
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

        [RelayCommand]
        public async Task WindowClosingAsync(CancelEventArgs e)
        {
            ProfileContainer profileContainer = new ProfileContainer();

            Task task1 = Task.Run(() =>
            {
                foreach (var item in this.Profiles)
                {
                    profileContainer.Profiles.Add(item._profile.Name, item._profile);
                }
            });

            await task1;

            this.profilesService.SaveProfiles(profileContainer);
        }
    }
}


