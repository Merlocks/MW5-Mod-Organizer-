using System.Text.Json.Serialization;

namespace MW5_Mod_Organizer_WPF.Models
{
    public class Mod
    {
        [JsonPropertyName("displayName")]
        [JsonPropertyOrder(0)]
        public string? DisplayName { get; set; }

        [JsonPropertyName("version")]
        [JsonPropertyOrder(1)]
        public string? Version { get; set; }

        [JsonPropertyName("buildNumber")]
        [JsonPropertyOrder(2)]
        public int? BuildNumber { get; set; }

        [JsonPropertyName("description")]
        [JsonPropertyOrder(3)]
        public string? Description { get; set; }

        [JsonPropertyName("author")]
        [JsonPropertyOrder(4)]
        public string? Author { get; set; }

        [JsonPropertyName("authorURL")]
        [JsonPropertyOrder(5)]
        public string? AuthorUrl { get; set; }

        [JsonPropertyName("defaultLoadOrder")]
        [JsonPropertyOrder(6)]
        public decimal? LoadOrder { get; set; }

        [JsonPropertyName("gameVersion")]
        [JsonPropertyOrder(7)]
        public string? GameVersion { get; set; }

        [JsonPropertyName("manifest")]
        [JsonPropertyOrder(8)]
        public string[]? Manifest { get; set; }

        [JsonPropertyName("steamPublishedFileId")]
        [JsonPropertyOrder(9)]
        public long? SteamPublishedFileId { get; set; }

        [JsonPropertyName("steamLastSubmittedBuildNumber")]
        [JsonPropertyOrder(10)]
        public int? SteamLastSubmittedBuildNumber { get; set; }

        [JsonPropertyName("steamModVisibility")]
        [JsonPropertyOrder(11)]
        public string? SteamModVisibility { get; set; }

        [JsonPropertyName("bEnabled")]
        [JsonPropertyOrder(12)]
        public bool IsEnabled { get; set; }

        [JsonPropertyName("originalLoadOrder")]
        [JsonPropertyOrder(13)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? OriginalLoadOrder { get; set; }

        [JsonIgnore]
        public string? Path { get; set; }

        [JsonIgnore]
        public string? FolderName { get; set; }

        [JsonIgnore]
        public string? Conflicts { get; set; }
    }
}
