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
using System.Windows.Forms;

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
            if (!string.IsNullOrEmpty(this.TextBoxContent) && !this.mainViewModel.DeploymentNecessary)
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

                    // Wait on completion of task.
                    task.Wait();

                    // Deselect selected profile
                    foreach (var item in this.Profiles.Where(p => !p.IsSelected)) item.IsSelected = false;

                    // Add and select new profile to list
                    ProfileViewModel? p = this.Profiles.Where(p => p.Name == this.TextBoxContent).SingleOrDefault();

                    if (p != null)
                    {
                        this.Profiles.Remove(p);
                        p = null;
                    }

                    ProfileViewModel addedProfileVM = new ProfileViewModel(profile);
                    this.Profiles.Add(addedProfileVM);

                    // Select added profile
                    addedProfileVM.IsSelected = true;

                    // Set current profile to newly created profile
                    this.mainViewModel.CurrentProfile = this.TextBoxContent;

                    Properties.Settings.Default.CurrentProfile = this.mainViewModel.CurrentProfile;
                    Properties.Settings.Default.Save();

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
            else if (this.mainViewModel.DeploymentNecessary)
            {
                string message = $"You must deploy your loadorder first before creating a profile.";
                string caption = "Warning";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                MessageBoxIcon icon = MessageBoxIcon.Warning;

                System.Windows.Forms.MessageBox.Show(message, caption, buttons, icon);
            }
        }

        [RelayCommand]
        public void DeleteProfile()
        {
            try
            {
                ProfileViewModel? selectedProfile = Profiles.Where(p => p.IsSelected).SingleOrDefault();

                if (selectedProfile != null)
                {
                    if (this.mainViewModel.CurrentProfile.Equals(selectedProfile.Name))
                    {
                        this.mainViewModel.CurrentProfile = string.Empty;
                        Properties.Settings.Default.CurrentProfile = string.Empty;
                        Properties.Settings.Default.Save();
                    }
                    else if (Properties.Settings.Default.CurrentProfile.Equals(selectedProfile.Name))
                    {
                        Properties.Settings.Default.CurrentProfile = string.Empty;
                        Properties.Settings.Default.Save();
                    }
                    
                    Profiles.Remove(selectedProfile);
                }

            } catch (Exception e)
            {

                Console.WriteLine($"-- ProfilesViewModel.SaveProfile -- {e.Message}");
            }
        }

        [RelayCommand]
        public void ActivateProfile(object sender)
        {
            try
            {
                ProfileViewModel? selectedProfile = Profiles.Where(p => p.IsSelected).SingleOrDefault();
                RaisableObservableCollection<ModViewModel> collection = this.mainViewModel.ModVMCollection;
                List<ModViewModel> modsOutsideProfileScope = new List<ModViewModel>();
                List<string> modsInsideProfileScope = new List<string>();

                if (selectedProfile != null && selectedProfile.Name != this.mainViewModel.CurrentProfile)
                {
                    foreach (var item in CollectionsMarshal.AsSpan(collection.ToList()))
                    {
                        if (item.FolderName != null && selectedProfile._profile.Entries.ContainsKey(item.FolderName))
                        {
                            item.IsEnabled = selectedProfile._profile.Entries.GetValueOrDefault(item.FolderName)!.IsEnabled;
                            item.LoadOrder = selectedProfile._profile.Entries.GetValueOrDefault(item.FolderName)!.LoadOrder;
                            modsInsideProfileScope.Add(item.FolderName);
                        }
                        else
                        {
                            item.IsEnabled = false;
                            modsOutsideProfileScope.Add(item);
                        }
                    }

                    this.mainViewModel.ModVMCollection = new RaisableObservableCollection<ModViewModel>(collection
                        .OrderByDescending(m => modsOutsideProfileScope.Contains(m))
                        .ThenBy(m => m.LoadOrder)
                        .ThenBy(m => m.FolderName));
                    this.mainViewModel.DeploymentNecessary = true;
                    this.mainViewModel.CurrentProfile = selectedProfile.Name;
                    this.mainViewModel.RaiseCheckForAllConflict();

                    // Close window
                    Window? window = sender as Window;

                    if (window != null)
                    {
                        window.Close();
                    }

                    // Notify user of any mods included in profile but don't exist in mods folder.
                    if (modsInsideProfileScope.Count < selectedProfile._profile.Entries.Count)
                    {
                        string info = "";
                        
                        foreach (var item in selectedProfile._profile.Entries.Where(m => !modsInsideProfileScope.Contains(m.Key)))
                        {
                            info += $"- {item.Key}\n";
                        }

                        string message = $"Certain mod(s) could not be activated because they don't exist:\n\n{info}";
                        string caption = "Warning";
                        MessageBoxButtons buttons = MessageBoxButtons.OK;
                        MessageBoxIcon icon = MessageBoxIcon.Warning;

                        System.Windows.Forms.MessageBox.Show(message, caption, buttons, icon);
                    }
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


