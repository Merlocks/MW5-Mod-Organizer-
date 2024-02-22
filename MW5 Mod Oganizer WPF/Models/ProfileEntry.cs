using System.Text.Json.Serialization;

namespace MW5_Mod_Organizer_WPF.Models
{
    public class ProfileEntry
    {
        [JsonPropertyOrder(1)]
        [JsonPropertyName("displayName")]
        public string? DisplayName;

        [JsonPropertyOrder(2)]
        [JsonPropertyName("defaultLoadOrder")]
        public decimal? LoadOrder;

        public ProfileEntry() 
        { 

        }
    }
}
