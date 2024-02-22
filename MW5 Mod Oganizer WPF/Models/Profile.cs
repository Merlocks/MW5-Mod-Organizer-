using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MW5_Mod_Organizer_WPF.Models
{
    public class Profile
    {
        [JsonPropertyName("mods")]
        public Dictionary<string, ProfileEntry> Mod;

        public Profile() 
        { 
            Mod = new Dictionary<string, ProfileEntry>();
        }
    }
}
