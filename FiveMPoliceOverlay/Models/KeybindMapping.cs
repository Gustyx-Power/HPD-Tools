using System;
using System.Text.Json.Serialization;

namespace FiveMPoliceOverlay.Models
{
    /// <summary>
    /// Maps a keybind to a message template
    /// </summary>
    public class KeybindMapping
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("keybind")]
        public KeybindDefinition Keybind { get; set; } = new();

        [JsonPropertyName("templateId")]
        public string TemplateId { get; set; } = string.Empty;
    }
}
