using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace MW5_Mod_Organizer_WPF.Models
{
    public sealed class ProfileContainer
    {
        [JsonPropertyName("profiles")]
        public Dictionary<string, Profile> Profiles { get; set; }

        public ProfileContainer() 
        { 
            this.Profiles = new Dictionary<string, Profile>();
        }
    }
}
