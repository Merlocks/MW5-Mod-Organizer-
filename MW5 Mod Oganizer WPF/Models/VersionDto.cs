using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MW5_Mod_Organizer_WPF.Models
{
    public sealed class VersionDto
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }
    }
}
