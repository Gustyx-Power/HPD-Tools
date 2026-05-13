using System;
using System.Windows;
using System.Windows.Input;
using FiveMPoliceOverlay.Models;

namespace FiveMPoliceOverlay.Services
{
    /// <summary>
    /// Unit tests for OverlayManager service.
    /// Tests overlay lifecycle, visibility toggling, and configuration persistence.
    /// </summary>
    public static class OverlayManagerTest
    {
        public static void RunTests()
        {
            Console.WriteLine("=== OverlayManager Tests ===\n");

            TestConstructor();
            TestShowOverlay();
            TestHideOverlay();
            TestToggleOverlay();
            TestRegisterToggleKeybind();
            TestVisibilityStatePersistence();
            TestPositionPersistence();
            TestDispose();

            Console.WriteLine("\n=== All OverlayManager Tests Completed ===");
        }

        private static void TestConstructor()
        {
            Console.WriteLine("Test: Constructor");
            
            var configManager = new ConfigurationManager();
            var keybindManager = new KeybindManager();
            var overlayManager = new OverlayManager(configManager, keybindManager);

            Assert(overlayManager != null, "OverlayManager should be created");
            Assert(!overlayManager.IsVisible(), "Overlay should not be visible initially");
            Assert(overlayManager.GetOverlayWindow() == null, "Overlay window should not exist initially");

            overlayManager.Dispose();
            keybindManager.Dispose();
            
            Console.WriteLine("✓ Constructor test passed\n");
        }

        private static void TestShowOverlay()
        {
            Console.WriteLine("Test: ShowOverlay");
            
            var configManager = new ConfigurationManager();
            var keybindManager = new KeybindManager();
            var overlayManager = new OverlayManager(configManager, keybindManager);

            // Show overlay
            overlayManager.ShowOverlay();

            Assert(overlayManager.IsVisible(), "Overlay should be visible after ShowOverlay");
            Assert(overlayManager.GetOverlayWindow() != null, "Overlay window should exist after ShowOverlay");

            // Verify position loaded from configuration
            var config = configManager.LoadConfiguration();
            var window = overlayManager.GetOverlayWindow();
            
            Assert(window != null, "Overlay window should not be null after ShowOverlay");
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                Assert(Math.Abs(window!.Left - config.Overlay.Position.X) < 1, 
                    $"Overlay X position should match config: expected {config.Overlay.Position.X}, got {window.Left}");
                Assert(Math.Abs(window.Top - config.Overlay.Position.Y) < 1, 
                    $"Overlay Y position should match config: expected {config.Overlay.Position.Y}, got {window.Top}");
            });

            overlayManager.Dispose();
            keybindManager.Dispose();
            
            Console.WriteLine("✓ ShowOverlay test passed\n");
        }

        private static void TestHideOverlay()
        {
            Console.WriteLine("Test: HideOverlay");
            
            var configManager = new ConfigurationManager();
            var keybindManager = new KeybindManager();
            var overlayManager = new OverlayManager(configManager, keybindManager);

            // Show then hide overlay
            overlayManager.ShowOverlay();
            Assert(overlayManager.IsVisible(), "Overlay should be visible after ShowOverlay");

            overlayManager.HideOverlay();
            Assert(!overlayManager.IsVisible(), "Overlay should not be visible after HideOverlay");
            Assert(overlayManager.GetOverlayWindow() != null, "Overlay window should still exist after HideOverlay");

            overlayManager.Dispose();
            keybindManager.Dispose();
            
            Console.WriteLine("✓ HideOverlay test passed\n");
        }

        private static void TestToggleOverlay()
        {
            Console.WriteLine("Test: ToggleOverlay");
            
            var configManager = new ConfigurationManager();
            var keybindManager = new KeybindManager();
            var overlayManager = new OverlayManager(configManager, keybindManager);

            // Initial state: not visible
            Assert(!overlayManager.IsVisible(), "Overlay should not be visible initially");

            // Toggle to visible
            overlayManager.ToggleOverlay();
            Assert(overlayManager.IsVisible(), "Overlay should be visible after first toggle");

            // Toggle to hidden
            overlayManager.ToggleOverlay();
            Assert(!overlayManager.IsVisible(), "Overlay should be hidden after second toggle");

            // Toggle to visible again
            overlayManager.ToggleOverlay();
            Assert(overlayManager.IsVisible(), "Overlay should be visible after third toggle");

            overlayManager.Dispose();
            keybindManager.Dispose();
            
            Console.WriteLine("✓ ToggleOverlay test passed\n");
        }

        private static void TestRegisterToggleKeybind()
        {
            Console.WriteLine("Test: RegisterToggleKeybind");
            
            var configManager = new ConfigurationManager();
            var keybindManager = new KeybindManager();
            var overlayManager = new OverlayManager(configManager, keybindManager);

            // Register toggle keybind
            overlayManager.RegisterToggleKeybind();

            // Verify keybind is registered
            var config = configManager.LoadConfiguration();
            var toggleKeybind = config.Overlay.ToggleKeybind;
            
            Assert(!keybindManager.IsKeybindAvailable(toggleKeybind), 
                "Toggle keybind should be registered (not available)");

            // Verify default keybind is Ctrl+F10
            Assert(toggleKeybind.Modifiers == ModifierKeys.Control, 
                "Default toggle keybind should have Control modifier");
            Assert(toggleKeybind.Key == Key.F10, 
                "Default toggle keybind should be F10");

            overlayManager.Dispose();
            keybindManager.Dispose();
            
            Console.WriteLine("✓ RegisterToggleKeybind test passed\n");
        }

        private static void TestVisibilityStatePersistence()
        {
            Console.WriteLine("Test: Visibility State Persistence");
            
            var configManager = new ConfigurationManager();
            var keybindManager = new KeybindManager();
            var overlayManager = new OverlayManager(configManager, keybindManager);

            // Show overlay and verify visibility state is saved
            overlayManager.ShowOverlay();
            System.Threading.Thread.Sleep(100); // Wait for async save

            var config = configManager.LoadConfiguration();
            Assert(config.Overlay.IsVisible, "Visibility state should be saved as true");

            // Hide overlay and verify visibility state is saved
            overlayManager.HideOverlay();
            System.Threading.Thread.Sleep(100); // Wait for async save

            config = configManager.LoadConfiguration();
            Assert(!config.Overlay.IsVisible, "Visibility state should be saved as false");

            overlayManager.Dispose();
            keybindManager.Dispose();
            
            Console.WriteLine("✓ Visibility State Persistence test passed\n");
        }

        private static void TestPositionPersistence()
        {
            Console.WriteLine("Test: Position Persistence");
            
            var configManager = new ConfigurationManager();
            var keybindManager = new KeybindManager();
            var overlayManager = new OverlayManager(configManager, keybindManager);

            // Show overlay
            overlayManager.ShowOverlay();

            // Update position
            var newPosition = new Point(100, 200);
            overlayManager.UpdatePosition(newPosition);
            System.Threading.Thread.Sleep(100); // Wait for async save

            // Verify position is saved
            var config = configManager.LoadConfiguration();
            Assert(Math.Abs(config.Overlay.Position.X - newPosition.X) < 1, 
                $"Position X should be saved: expected {newPosition.X}, got {config.Overlay.Position.X}");
            Assert(Math.Abs(config.Overlay.Position.Y - newPosition.Y) < 1, 
                $"Position Y should be saved: expected {newPosition.Y}, got {config.Overlay.Position.Y}");

            overlayManager.Dispose();
            keybindManager.Dispose();
            
            Console.WriteLine("✓ Position Persistence test passed\n");
        }

        private static void TestDispose()
        {
            Console.WriteLine("Test: Dispose");
            
            var configManager = new ConfigurationManager();
            var keybindManager = new KeybindManager();
            var overlayManager = new OverlayManager(configManager, keybindManager);

            // Show overlay
            overlayManager.ShowOverlay();
            Assert(overlayManager.GetOverlayWindow() != null, "Overlay window should exist");

            // Dispose
            overlayManager.Dispose();

            // Verify overlay window is disposed
            Assert(overlayManager.GetOverlayWindow() == null, "Overlay window should be null after dispose");

            keybindManager.Dispose();
            
            Console.WriteLine("✓ Dispose test passed\n");
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
