using MW5_Mod_Organizer_WPF.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

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

                if (!Directory.Exists(@"Userdata"))
                {
                    Directory.CreateDirectory(@"Userdata");
                }

                if (!File.Exists(@"Userdata\profiles.json"))
                {
                    await File.WriteAllTextAsync(@"Userdata\profiles.json", "");
                }

                string? jsonString = await File.ReadAllTextAsync(@"Userdata\profiles.json");
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
                if (!Directory.Exists(@"Userdata"))
                {
                    Directory.CreateDirectory(@"Userdata");
                }

                if (!File.Exists(@"Userdata\profiles.json"))
                {
                    File.WriteAllText(@"Userdata\profiles.json", "");
                }

                var options = new JsonSerializerOptions() { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(profileContainer, options);

                File.WriteAllText(@"Userdata\profiles.json", jsonString);
            }
            catch (Exception e)
            {
                Console.WriteLine($"-- ProfilesService.SaveProfiles -- {e.Message}");
            }
        }
    }
}
