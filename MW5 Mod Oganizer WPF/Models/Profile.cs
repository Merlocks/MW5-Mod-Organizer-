using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MW5_Mod_Organizer_WPF.Models
{
    public sealed class Profile
    {
        [JsonIgnore]
        public string Name { get; set; }
        
        [JsonPropertyName("mods")]
        [JsonPropertyOrder(0)]
        public Dictionary<string, bool> Entries { get; set; }

        public Profile(string name) 
        { 
            this.Name = name;
            this.Entries = new Dictionary<string, bool>();
        }
    }
}
