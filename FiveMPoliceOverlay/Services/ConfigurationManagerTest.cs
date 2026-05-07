using System;
using System.IO;
using System.Linq;
using FiveMPoliceOverlay.Models;

namespace FiveMPoliceOverlay.Services
{
    /// <summary>
    /// Simple test class to verify ConfigurationManager functionality
    /// This is a basic validation test, not a full unit test suite
    /// </summary>
    public class ConfigurationManagerTest
    {
        public static void RunTests()
        {
            Console.WriteLine("=== ConfigurationManager Tests ===\n");

            TestDefaultConfiguration();
            TestSaveAndLoad();
            TestCorruptedFileHandling();
            TestResetToDefaults();

            Console.WriteLine("\n=== All Tests Completed ===");
        }

        private static void TestDefaultConfiguration()
        {
            Console.WriteLine("Test 1: Default Configuration Creation");
            
            var manager = new ConfigurationManager();
            var config = manager.ResetToDefaults();

            // Verify version
            Assert(config.Version == "1.0", "Version should be 1.0");

            // Verify general settings
            Assert(config.General.AutoLaunch == true, "AutoLaunch should be true");
            Assert(config.General.TestMode == false, "TestMode should be false");
            Assert(config.General.Language == "id-ID", "Language should be id-ID");

            // Verify overlay settings
            Assert(config.Overlay.Position.X == 10, "Overlay X position should be 10");
            Assert(config.Overlay.Position.Y == 100, "Overlay Y position should be 100");
            Assert(config.Overlay.IsVisible == true, "Overlay should be visible");

            // Verify rate limiting
            Assert(config.RateLimiting.CooldownSeconds == 2, "Cooldown should be 2 seconds");
            Assert(config.RateLimiting.MaxQueueSize == 5, "Max queue size should be 5");

            // Verify 4 predefined templates exist
            Assert(config.Templates.Count == 4, "Should have 4 predefined templates");

            var siaga3 = config.Templates.FirstOrDefault(t => t.Id == "tpl-siaga-3");
            Assert(siaga3 != null, "Siaga III template should exist");
            Assert(siaga3!.Name == "Siaga III", "Siaga III name should match");
            Assert(siaga3.Category == "BERITA LANGIT KOTA SIAGA", "Category should be BERITA LANGIT KOTA SIAGA");
            Assert(siaga3.IsPredefined == true, "Should be marked as predefined");
            Assert(siaga3.Text.Contains("SIAGA III"), "Text should contain SIAGA III");

            var siaga2 = config.Templates.FirstOrDefault(t => t.Id == "tpl-siaga-2");
            Assert(siaga2 != null, "Siaga II template should exist");
            Assert(siaga2!.Name == "Siaga II", "Siaga II name should match");

            var siaga1 = config.Templates.FirstOrDefault(t => t.Id == "tpl-siaga-1");
            Assert(siaga1 != null, "Siaga I template should exist");
            Assert(siaga1!.Name == "Siaga I", "Siaga I name should match");

            var siagaTotal = config.Templates.FirstOrDefault(t => t.Id == "tpl-siaga-total");
            Assert(siagaTotal != null, "Siaga Total template should exist");
            Assert(siagaTotal!.Name == "Siaga Total", "Siaga Total name should match");
            Assert(siagaTotal.Text.Contains("SIAGA TOTAL"), "Text should contain SIAGA TOTAL");

            Console.WriteLine("✓ Default configuration test passed\n");
        }

        private static void TestSaveAndLoad()
        {
            Console.WriteLine("Test 2: Save and Load Configuration");

            var manager = new ConfigurationManager();
            
            // Create a custom configuration
            var config = manager.ResetToDefaults();
            config.General.TestMode = true;
            config.Overlay.Position = new System.Windows.Point(50, 200);

            // Save configuration
            manager.SaveConfiguration(config).Wait();

            // Load configuration
            var loadedConfig = manager.LoadConfiguration();

            // Verify loaded values match saved values
            Assert(loadedConfig.General.TestMode == true, "TestMode should be true after load");
            Assert(loadedConfig.Overlay.Position.X == 50, "X position should be 50 after load");
            Assert(loadedConfig.Overlay.Position.Y == 200, "Y position should be 200 after load");
            Assert(loadedConfig.Templates.Count == 4, "Should still have 4 templates after load");

            Console.WriteLine("✓ Save and load test passed\n");
        }

        private static void TestCorruptedFileHandling()
        {
            Console.WriteLine("Test 3: Corrupted File Handling");

            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FiveMPoliceOverlay",
                "config.json"
            );

            // Write corrupted JSON to config file
            File.WriteAllText(configPath, "{ invalid json content !!!");

            var manager = new ConfigurationManager();
            var config = manager.LoadConfiguration();

            // Should return default configuration
            Assert(config != null, "Should return valid config even with corrupted file");
            Assert(config!.Templates.Count == 4, "Should have default templates");

            // Verify backup was created
            var backupPath = configPath + ".bak";
            Assert(File.Exists(backupPath), "Backup file should exist");

            Console.WriteLine("✓ Corrupted file handling test passed\n");
        }

        private static void TestResetToDefaults()
        {
            Console.WriteLine("Test 4: Reset to Defaults");

            var manager = new ConfigurationManager();
            
            // Load and modify configuration
            var config = manager.LoadConfiguration();
            config.General.TestMode = true;
            config.Templates.Clear();
            manager.SaveConfiguration(config).Wait();

            // Reset to defaults
            var resetConfig = manager.ResetToDefaults();

            // Verify reset worked
            Assert(resetConfig.General.TestMode == false, "TestMode should be false after reset");
            Assert(resetConfig.Templates.Count == 4, "Should have 4 templates after reset");

            // Load again to verify persistence
            var loadedConfig = manager.LoadConfiguration();
            Assert(loadedConfig.General.TestMode == false, "TestMode should be false after reload");
            Assert(loadedConfig.Templates.Count == 4, "Should have 4 templates after reload");

            Console.WriteLine("✓ Reset to defaults test passed\n");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Assertion failed: {message}");
            }
        }
    }
}
