using System;
using System.Text.Json.Serialization;

namespace FiveMPoliceOverlay.Models
{
    /// <summary>
    /// Template for broadcast messages
    /// </summary>
    public class MessageTemplate
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("isPredefined")]
        public bool IsPredefined { get; set; } = false;
    }
}
