using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;

namespace FiveMPoliceOverlay.Models
{
    /// <summary>
    /// Overlay window settings
    /// </summary>
    public class OverlaySettings
    {
        [JsonPropertyName("position")]
        [JsonConverter(typeof(PointJsonConverter))]
        public Point Position { get; set; } = new(10, 100);

        [JsonPropertyName("isVisible")]
        public bool IsVisible { get; set; } = true;

        [JsonPropertyName("toggleKeybind")]
        public KeybindDefinition ToggleKeybind { get; set; } = new()
        {
            Modifiers = ModifierKeys.Control,
            Key = Key.F10
        };
    }
}
