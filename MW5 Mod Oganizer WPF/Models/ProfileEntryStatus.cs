using System.Text.Json.Serialization;

namespace MW5_Mod_Organizer_WPF.Models
{
    public sealed class ProfileEntryStatus
    {
        [JsonPropertyName("bEnabled")]
        [JsonPropertyOrder(0)]
        public bool IsEnabled { get; set; }

        [JsonPropertyName("defaultLoadOrder")]
        [JsonPropertyOrder(1)]
        public decimal LoadOrder { get; set; }
        
        public ProfileEntryStatus() { }
    }
}
