using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FiveMPoliceOverlay.Models
{
    /// <summary>
    /// Root configuration model for the application
    /// </summary>
    public class AppConfiguration
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        [JsonPropertyName("general")]
        public GeneralSettings General { get; set; } = new();

        [JsonPropertyName("overlay")]
        public OverlaySettings Overlay { get; set; } = new();

        [JsonPropertyName("keybinds")]
        public List<KeybindMapping> Keybinds { get; set; } = new();

        [JsonPropertyName("templates")]
        public List<MessageTemplate> Templates { get; set; } = new();

        [JsonPropertyName("rateLimiting")]
        public RateLimitSettings RateLimiting { get; set; } = new();
    }
}
