using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FiveMPoliceOverlay.Models;

namespace FiveMPoliceOverlay.Services
{
    /// <summary>
    /// Manages application configuration loading, saving, and defaults
    /// Handles JSON serialization and error recovery for corrupted config files
    /// </summary>
    public class ConfigurationManager
    {
        private readonly string _configDirectory;
        private readonly string _configFilePath;
        private readonly JsonSerializerOptions _jsonOptions;

        public ConfigurationManager()
        {
            // Configuration stored in %APPDATA%\FiveMPoliceOverlay\config.json
            _configDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FiveMPoliceOverlay"
            );
            _configFilePath = Path.Combine(_configDirectory, "config.json");

            // Configure JSON serialization options
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Loads configuration from disk. Creates default configuration if file doesn't exist.
        /// If file is corrupted, backs it up to .bak and creates new default configuration.
        /// </summary>
        /// <returns>Loaded or default AppConfiguration</returns>
        public AppConfiguration LoadConfiguration()
        {
            try
            {
                // Ensure config directory exists
                if (!Directory.Exists(_configDirectory))
                {
                    Directory.CreateDirectory(_configDirectory);
                }

                // If config file doesn't exist, create default
                if (!File.Exists(_configFilePath))
                {
                    var defaultConfig = CreateDefaultConfiguration();
                    SaveConfiguration(defaultConfig).Wait(); // Synchronous wait for initial save
                    return defaultConfig;
                }

                // Read and deserialize config file
                string jsonContent = File.ReadAllText(_configFilePath);
                var config = JsonSerializer.Deserialize<AppConfiguration>(jsonContent, _jsonOptions);

                if (config == null)
                {
                    throw new JsonException("Deserialized configuration is null");
                }

                // Auto-migrate default ToggleKeybind from Ctrl+F10 to F10 for in-game convenience
                if (config.Overlay.ToggleKeybind.Modifiers == ModifierKeys.Control && config.Overlay.ToggleKeybind.Key == Key.F10)
                {
                    config.Overlay.ToggleKeybind.Modifiers = ModifierKeys.None;
                    SaveConfiguration(config).Wait();
                    Console.WriteLine("[ConfigurationManager] Auto-migrated default toggle keybind from Ctrl+F10 to F10");
                }

                return config;
            }
            catch (JsonException ex)
            {
                // Config file is corrupted - backup and reset
                return HandleCorruptedConfig(ex);
            }
            catch (Exception ex)
            {
                // Other errors (file access, etc.) - log and return default
                Console.WriteLine($"Error loading configuration: {ex.Message}");
                return CreateDefaultConfiguration();
            }
        }

        /// <summary>
        /// Saves configuration to disk asynchronously
        /// </summary>
        /// <param name="config">Configuration to save</param>
        public async Task SaveConfiguration(AppConfiguration config)
        {
            try
            {
                // Ensure config directory exists
                if (!Directory.Exists(_configDirectory))
                {
                    Directory.CreateDirectory(_configDirectory);
                }

                // Serialize to JSON
                string jsonContent = JsonSerializer.Serialize(config, _jsonOptions);

                // Write to file asynchronously avoiding UI thread deadlock
                await File.WriteAllTextAsync(_configFilePath, jsonContent).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Resets configuration to default values with predefined BERITA LANGIT KOTA SIAGA templates
        /// </summary>
        /// <returns>Default AppConfiguration</returns>
        public AppConfiguration ResetToDefaults()
        {
            var defaultConfig = CreateDefaultConfiguration();
            SaveConfiguration(defaultConfig).Wait(); // Synchronous wait for reset
            return defaultConfig;
        }

        /// <summary>
        /// Creates default configuration with 4 predefined BERITA LANGIT KOTA SIAGA templates
        /// </summary>
        private AppConfiguration CreateDefaultConfiguration()
        {
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
                        Modifiers = ModifierKeys.None,
                        Key = Key.F10
                    }
                },
                RateLimiting = new RateLimitSettings
                {
                    CooldownSeconds = 2,
                    MaxQueueSize = 5
                }
            };

            // Add 4 predefined BERITA LANGIT KOTA SIAGA templates
            config.Templates.Add(new MessageTemplate
            {
                Id = "tpl-siaga-3",
                Category = "BERITA LANGIT KOTA SIAGA",
                Name = "Siaga III",
                Text = "DI INFORMASIKAN KEPADA SELURUH WARGA AGAR SEGERA MENGAMANKAN DIRI DAN MENCARI TEMPAT AMAN DIKARENAKAN KOTA MENGALAMI SIAGA III TERIMAKASIH.",
                IsPredefined = true
            });

            config.Templates.Add(new MessageTemplate
            {
                Id = "tpl-siaga-2",
                Category = "BERITA LANGIT KOTA SIAGA",
                Name = "Siaga II",
                Text = "DI INFORMASIKAN KEPADA SELURUH WARGA AGAR SEGERA MENGAMANKAN DIRI DAN MENCARI TEMPAT AMAN DIKARENAKAN KOTA MENGALAMI SIAGA II. TERIMAKASIH.",
                IsPredefined = true
            });

            config.Templates.Add(new MessageTemplate
            {
                Id = "tpl-siaga-1",
                Category = "BERITA LANGIT KOTA SIAGA",
                Name = "Siaga I",
                Text = "DI INFORMASIKAN KEPADA SELURUH WARGA AGAR SEGERA MENGAMANKAN DIRI DAN MENCARI TEMPAT AMAN DIKARENAKAN KOTA MENGALAMI SIAGA I. TERIMAKASIH.",
                IsPredefined = true
            });

            config.Templates.Add(new MessageTemplate
            {
                Id = "tpl-siaga-total",
                Category = "BERITA LANGIT KOTA SIAGA",
                Name = "Siaga Total",
                Text = "DI INFORMASIKAN KEPADA SELURUH WARGA AGAR SEGERA MENGAMANKAN DIRI DAN MENCARI TEMPAT AMAN DIKARENAKAN MARAKNYA TINDAK KRIMINAL DAN SUDAH TIDAK KONDUSIF MAKA KOTA DITINGKATKAN KE SIAGA TOTAL. TERIMAKASIH.",
                IsPredefined = true
            });

            return config;
        }

        /// <summary>
        /// Handles corrupted configuration file by backing it up and creating new default
        /// </summary>
        private AppConfiguration HandleCorruptedConfig(Exception ex)
        {
            try
            {
                Console.WriteLine($"Configuration file corrupted: {ex.Message}");

                // Backup corrupted file to .bak
                string backupPath = _configFilePath + ".bak";
                if (File.Exists(_configFilePath))
                {
                    // If backup already exists, delete it first
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }
                    File.Move(_configFilePath, backupPath);
                    Console.WriteLine($"Corrupted config backed up to: {backupPath}");
                }

                // Create and save default configuration
                var defaultConfig = CreateDefaultConfiguration();
                SaveConfiguration(defaultConfig).Wait();
                Console.WriteLine("Configuration reset to defaults");

                return defaultConfig;
            }
            catch (Exception backupEx)
            {
                Console.WriteLine($"Error handling corrupted config: {backupEx.Message}");
                // If backup fails, just return default without saving
                return CreateDefaultConfiguration();
            }
        }
    }
}
