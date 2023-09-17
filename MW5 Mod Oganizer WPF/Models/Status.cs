using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MW5_Mod_Organizer_WPF.Models
{
    public class Status
    {
        [JsonPropertyName("bEnabled")]
        public bool IsEnabled { get; set; }
    }
}
