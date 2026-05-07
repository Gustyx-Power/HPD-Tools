using System.Text.Json.Serialization;

namespace FiveMPoliceOverlay.Models
{
    /// <summary>
    /// Rate limiting configuration to prevent message spam
    /// </summary>
    public class RateLimitSettings
    {
        [JsonPropertyName("cooldownSeconds")]
        public int CooldownSeconds { get; set; } = 2;

        [JsonPropertyName("maxQueueSize")]
        public int MaxQueueSize { get; set; } = 5;
    }
}
