using MW5_Mod_Organizer_WPF.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MW5_Mod_Organizer_WPF.Services
{
    public sealed class ProfilesService
    {
        public ProfilesService()
        {
        }

        /// <summary>
        /// Retrieves all profiles from file and returns as ProfileContainer.
        /// </summary>
        /// <returns></returns>
        public async Task<ProfileContainer> GetProfilesAsync()
        {
            try
            {
                ProfileContainer? profileContainer = new ProfileContainer();

                if (!Directory.Exists(Environment.GetEnvironmentVariable("LocalAppData") + @"\MW5Mercs\Saved\MW5MO"))
                {
                    Directory.CreateDirectory(Environment.GetEnvironmentVariable("LocalAppData") + @"\MW5Mercs\Saved\MW5MO");
                }

                if (!File.Exists(Environment.GetEnvironmentVariable("LocalAppData") + @"\MW5Mercs\Saved\MW5MO\profiles.json"))
                {
                    await File.WriteAllTextAsync(Environment.GetEnvironmentVariable("LocalAppData") + @"\MW5Mercs\Saved\MW5MO\profiles.json", "");
                }

                string? jsonString = await File.ReadAllTextAsync(Environment.GetEnvironmentVariable("LocalAppData") + @"\MW5Mercs\Saved\MW5MO\profiles.json");
                profileContainer = JsonSerializer.Deserialize<ProfileContainer>(jsonString);

                if (profileContainer != null && profileContainer.Profiles.Count != 0)
                {
                    foreach (var item in profileContainer.Profiles) { item.Value.Name = item.Key; }

                    return profileContainer;
                }
                else
                {
                    return new ProfileContainer();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"-- ProfilesService.GetProfilesAsync -- {e.Message}");
                return new ProfileContainer();
            }
        }

        /// <summary>
        /// Saves all profiles from parameter to file.
        /// </summary>
        /// <param name="profileContainer"></param>
        public void SaveProfiles(ProfileContainer profileContainer)
        {
            try
            {
                if (!Directory.Exists(Environment.GetEnvironmentVariable("LocalAppData") + @"\MW5Mercs\Saved\MW5MO"))
                {
                    Directory.CreateDirectory(Environment.GetEnvironmentVariable("LocalAppData") + @"\MW5Mercs\Saved\MW5MO");
                }

                if (!File.Exists(Environment.GetEnvironmentVariable("LocalAppData") + @"\MW5Mercs\Saved\MW5MO\profiles.json"))
                {
                    File.WriteAllText(Environment.GetEnvironmentVariable("LocalAppData") + @"\MW5Mercs\Saved\MW5MO\profiles.json", "");
                }

                var options = new JsonSerializerOptions() { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(profileContainer, options);

                File.WriteAllText(Environment.GetEnvironmentVariable("LocalAppData") + @"\MW5Mercs\Saved\MW5MO\profiles.json", jsonString);
            }
            catch (Exception e)
            {
                Console.WriteLine($"-- ProfilesService.SaveProfiles -- {e.Message}");
            }
        }

        public void ExportProfiles(Dictionary<string, Profile> profiles)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = $"Save Profiles",
                    FileName = $"exported-profiles",
                    Filter = "Json file (*.json)|*.json",
                    FilterIndex = 0,
                    DefaultExt = "exe",
                    RestoreDirectory = true
                };
                DialogResult result = dialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    JsonSerializerOptions options = new JsonSerializerOptions() { WriteIndented = true };
                    string jsonString = JsonSerializer.Serialize<Dictionary<string, Profile>>(profiles, options);

                    File.WriteAllText($"{dialog.FileName}", jsonString);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"-- ProfilesService.ExportProfiles -- {e.Message}");
            }
        }

        public Dictionary<string, Profile>? ImportProfiles()
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog()
                {
                    CheckPathExists = true,
                    Filter = "json files (*.json)|*.json",
                    RestoreDirectory = true,
                    Title = "Import Profiles"
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string jsonString = File.ReadAllText(dialog.FileName);

                    Dictionary<string, Profile>? profiles = JsonSerializer.Deserialize<Dictionary<string, Profile>>(jsonString);

                    if (profiles != null)
                    {
                        ProfileContainer profileContainer = new ProfileContainer();

                        Task getProfiles = Task.Run(() => 
                        {
                            profileContainer = this.GetProfilesAsync().Result;
                        });

                        getProfiles.Wait();

                        foreach (var item in profiles)
                        {
                            if (!profileContainer.Profiles.ContainsKey(item.Key))
                            {
                                profileContainer.Profiles.Add(item.Key, item.Value); 
                            }
                        }

                        this.SaveProfiles(profileContainer);

                        return profiles;
                    }
                }

                return null;
            } 
            catch (Exception e)
            {
                Console.WriteLine($"-- ProfilesService.ImportProfiles -- {e.Message}");
                return null;
            }
        }
    }
}
