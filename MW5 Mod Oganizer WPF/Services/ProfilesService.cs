using MW5_Mod_Organizer_WPF.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace MW5_Mod_Organizer_WPF.Services
{
    public class ProfilesService
    {
        public ProfilesService()
        {
        }

        public async Task<ProfileContainer> GetProfilesAsync()
        {
            // DEBUG TIMER
            var timer = Stopwatch.StartNew();
            
            ProfileContainer? profileContainer = new ProfileContainer();
            
            if (!Directory.Exists(@"Userdata"))
            {
                Directory.CreateDirectory(@"Userdata");
            }

            if (!File.Exists(@"Userdata\profiles.json"))
            {
                File.WriteAllText(@"Userdata\profiles.json", "");
            }

            try
            {
                string? jsonString = File.ReadAllText(@"Userdata\profiles.json");
                profileContainer = JsonSerializer.Deserialize<ProfileContainer>(jsonString);

                if (profileContainer != null && profileContainer.Profiles.Count != 0)
                {
                    return profileContainer;
                }
                else
                {
                    return new ProfileContainer();
                }
            }
            catch (System.Exception)
            {

                return new ProfileContainer();
            }
            finally
            {
                // DEBUG TIMER
                timer.Stop();
                var time = timer.ElapsedMilliseconds;

                Console.WriteLine($"GetProfiles elapsed debug time: {time}ms");
            }
        }

        public void SaveProfiles()
        {

        }
    }
}
