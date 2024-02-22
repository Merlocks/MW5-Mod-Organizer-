using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MW5_Mod_Organizer_WPF.Models
{
    public class ProfileContainer
    {
        [JsonPropertyName("profiles")]
        public Dictionary<string, Profile> Profile;

        public ProfileContainer() 
        { 
            Profile = new Dictionary<string, Profile>();
        }
    }
}
