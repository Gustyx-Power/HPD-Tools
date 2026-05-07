using System;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace FiveMPoliceOverlay.Models
{
    /// <summary>
    /// Simple test class to verify JSON serialization of configuration models
    /// This can be removed after proper unit tests are implemented
    /// </summary>
    public static class ConfigurationTest
    {
        public static void TestSerialization()
        {
            // Create a sample configuration
            var config = new AppConfiguration
            {
                Version = "1.0",
                General = new GeneralSettings
                {
                    AutoLaunch = true,
                    TestMode = false,
                    Language = "id-ID"
                },
                Overlay = new OverlaySettings
                {
                    Position = new Point(10, 100),
                    IsVisible = true,
                    ToggleKeybind = new KeybindDefinition
                    {
                        Modifiers = ModifierKeys.Control,
                        Key = Key.F10
                    }
                },
                RateLimiting = new RateLimitSettings
                {
                    CooldownSeconds = 2,
                    MaxQueueSize = 5
                }
            };

            // Add a sample keybind
            config.Keybinds.Add(new KeybindMapping
            {
                Id = "kb-001",
                Keybind = new KeybindDefinition
                {
                    Modifiers = ModifierKeys.Control | ModifierKeys.Shift,
                    Key = Key.D1
                },
                TemplateId = "tpl-siaga-3"
            });

            // Add a sample template
            config.Templates.Add(new MessageTemplate
            {
                Id = "tpl-siaga-3",
                Category = "BERITA LANGIT KOTA SIAGA",
                Name = "Siaga III",
                Text = "DI INFORMASIKAN KEPADA SELURUH WARGA AGAR SEGERA MENGAMANKAN DIRI DAN MENCARI TEMPAT AMAN DIKARENAKAN KOTA MENGALAMI SIAGA III TERIMAKASIH.",
                IsPredefined = true
            });

            // Serialize to JSON
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string json = JsonSerializer.Serialize(config, options);
            Console.WriteLine("Serialized JSON:");
            Console.WriteLine(json);

            // Deserialize back
            var deserialized = JsonSerializer.Deserialize<AppConfiguration>(json, options);
            Console.WriteLine("\nDeserialization successful!");
            Console.WriteLine($"Version: {deserialized?.Version}");
            Console.WriteLine($"Auto Launch: {deserialized?.General.AutoLaunch}");
            Console.WriteLine($"Overlay Position: {deserialized?.Overlay.Position}");
            Console.WriteLine($"Toggle Keybind: {deserialized?.Overlay.ToggleKeybind}");
            Console.WriteLine($"Keybinds Count: {deserialized?.Keybinds.Count}");
            Console.WriteLine($"Templates Count: {deserialized?.Templates.Count}");
        }
    }
}
