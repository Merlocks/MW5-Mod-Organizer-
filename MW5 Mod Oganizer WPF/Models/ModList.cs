using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MW5_Mod_Organizer_WPF.Models
{
    public class ModList
    {
        [JsonPropertyName("gameVersion")]
        [JsonPropertyOrder(0)]
        public string? GameVersion => Properties.Settings.Default.GameVersion;

        [JsonPropertyName("modStatus")]
        [JsonPropertyOrder(1)]
        public Dictionary<string, Status>? ModStatus { get; set; }
    }
}
