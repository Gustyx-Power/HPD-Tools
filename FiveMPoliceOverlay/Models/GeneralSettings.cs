using System.Text.Json.Serialization;

namespace FiveMPoliceOverlay.Models
{
    /// <summary>
    /// General application settings
    /// </summary>
    public class GeneralSettings
    {
        [JsonPropertyName("autoLaunch")]
        public bool AutoLaunch { get; set; } = true;

        [JsonPropertyName("testMode")]
        public bool TestMode { get; set; } = false;

        [JsonPropertyName("language")]
        public string Language { get; set; } = "id-ID";
    }
}
