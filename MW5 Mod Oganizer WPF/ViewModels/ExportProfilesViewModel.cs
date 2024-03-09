using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MW5_Mod_Organizer_WPF.Models;
using MW5_Mod_Organizer_WPF.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MW5_Mod_Organizer_WPF.ViewModels
{
    public sealed partial class ExportProfilesViewModel : ObservableObject
    {
        private MainViewModel mainViewModel => App.Current.Services.GetService<MainViewModel>()!;
        private readonly ProfilesService profilesService;

        [ObservableProperty]
        private ObservableCollection<ProfileViewModel> profiles;

        public ExportProfilesViewModel(ProfilesService profilesService)
        {
            this.profilesService = profilesService;

            profiles = new ObservableCollection<ProfileViewModel>();
        }

        [RelayCommand]
        public void SaveProfiles(object sender)
        {
            try
            {
                Window? window = sender as Window;
                Dictionary<string, Profile> selectedProfiles = new Dictionary<string, Profile>();

                foreach (var item in CollectionsMarshal.AsSpan(this.Profiles.Where(p => p.IsSelected).ToList()))
                {
                    selectedProfiles.Add(item._profile.Name, item._profile);
                }

                if (selectedProfiles.Count() > 0)
                {
                    this.profilesService.ExportProfiles(selectedProfiles);

                    if (window != null)
                    {
                        window.Close();
                    } 
                }

            } catch (Exception ex)
            {
                Console.WriteLine($"-- ExportProfilesViewModel.SaveProfiles -- {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task WindowLoadedAsync(RoutedEventArgs e)
        {
            try
            {
                ProfileContainer profileContainer = await this.profilesService.GetProfilesAsync();

                foreach (var item in profileContainer.Profiles.OrderBy(p => p.Key).ToArray())
                {
                    this.Profiles.Add(new ProfileViewModel(item.Value));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"-- ExportProfilesViewModel.WindowLoadedAsync -- {ex.Message}");
            }
            finally
            {
                e.Handled = true;
            }

        }
    }
}
