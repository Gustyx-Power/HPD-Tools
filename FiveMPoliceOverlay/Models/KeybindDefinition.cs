using System.Text.Json.Serialization;
using System.Windows.Input;

namespace FiveMPoliceOverlay.Models
{
    /// <summary>
    /// Defines a keyboard shortcut with modifiers and key
    /// </summary>
    public class KeybindDefinition
    {
        [JsonPropertyName("modifiers")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ModifierKeys Modifiers { get; set; } = ModifierKeys.None;

        [JsonPropertyName("key")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Key Key { get; set; } = Key.None;

        public override string ToString()
        {
            if (Modifiers == ModifierKeys.None)
                return Key.ToString();
            return $"{Modifiers}+{Key}";
        }
    }
}
