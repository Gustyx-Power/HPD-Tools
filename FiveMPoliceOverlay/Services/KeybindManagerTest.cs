using System;
using System.Windows.Input;
using FiveMPoliceOverlay.Models;

namespace FiveMPoliceOverlay.Services
{
    /// <summary>
    /// Manual tests for KeybindManager service.
    /// Tests keybind registration, validation, and duplicate detection.
    /// Note: Global keyboard hook functionality requires integration testing with actual keyboard input.
    /// Call RunTests() from your application to execute tests.
    /// </summary>
    public class KeybindManagerTest
    {
        #region Test Helpers

        private static KeybindDefinition CreateKeybind(ModifierKeys modifiers, Key key)
        {
            return new KeybindDefinition
            {
                Modifiers = modifiers,
                Key = key
            };
        }

        private static MessageTemplate CreateTemplate(string name, string text)
        {
            return new MessageTemplate
            {
                Id = Guid.NewGuid().ToString(),
                Category = "Test",
                Name = name,
                Text = text,
                IsPredefined = false
            };
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Assertion failed: {message}");
            }
        }

        #endregion

        #region Main Test Runner

        public static void RunTests()
        {
            Console.WriteLine("=== KeybindManager Test Suite ===\n");

            try
            {
                TestRegistration();
                TestUnregistration();
                TestAvailability();
                TestKeybindUniqueness();
                TestGetRegisteredKeybinds();
                TestClearAllKeybinds();
                TestEventSubscription();
                TestHookLifecycle();
                TestKeybindRegistrationClass();
                TestKeybindPressedEventArgs();

                Console.WriteLine("\n=== All Tests Completed Successfully ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[ERROR] Test failed: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        #endregion

        #region Registration Tests

        private static void TestRegistration()
        {
            Console.WriteLine("Test 1: Keybind Registration");
            Console.WriteLine("-----------------------------");

            using var manager = new KeybindManager();

            // Test 1.1: Valid keybind registration
            var keybind1 = CreateKeybind(ModifierKeys.Control, Key.F1);
            var template1 = CreateTemplate("Test Message", "This is a test");
            bool result1 = manager.RegisterKeybind(keybind1, template1);
            Assert(result1 == true, "Valid keybind should register successfully");

            // Test 1.2: Duplicate keybind registration
            var template2 = CreateTemplate("Message 2", "Second message");
            bool result2 = manager.RegisterKeybind(keybind1, template2);
            Assert(result2 == false, "Duplicate keybind should fail to register");

            // Test 1.3: Multiple modifiers
            var keybind2 = CreateKeybind(ModifierKeys.Control | ModifierKeys.Shift, Key.F1);
            var template3 = CreateTemplate("Test", "Test message");
            bool result3 = manager.RegisterKeybind(keybind2, template3);
            Assert(result3 == true, "Keybind with multiple modifiers should register");

            // Test 1.4: No modifiers
            var keybind3 = CreateKeybind(ModifierKeys.None, Key.F12);
            var template4 = CreateTemplate("Test", "Test message");
            bool result4 = manager.RegisterKeybind(keybind3, template4);
            Assert(result4 == true, "Keybind with no modifiers should register");

            // Test 1.5: Null keybind
            try
            {
                manager.RegisterKeybind(null!, template1);
                Assert(false, "Should throw ArgumentNullException for null keybind");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }

            // Test 1.6: Null template
            try
            {
                manager.RegisterKeybind(keybind1, null!);
                Assert(false, "Should throw ArgumentNullException for null template");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }

            Console.WriteLine("✓ Registration tests passed\n");
        }

        #endregion

        #region Unregistration Tests

        private static void TestUnregistration()
        {
            Console.WriteLine("Test 2: Keybind Unregistration");
            Console.WriteLine("-------------------------------");

            using var manager = new KeybindManager();

            // Test 2.1: Unregister existing keybind
            var keybind = CreateKeybind(ModifierKeys.Control, Key.F1);
            var template = CreateTemplate("Test", "Test message");
            manager.RegisterKeybind(keybind, template);
            bool result1 = manager.UnregisterKeybind(keybind);
            Assert(result1 == true, "Registered keybind should unregister successfully");

            // Test 2.2: Unregister non-existent keybind
            bool result2 = manager.UnregisterKeybind(keybind);
            Assert(result2 == false, "Unregistered keybind should return false");

            // Test 2.3: Re-registration after unregistration
            var template2 = CreateTemplate("Message 2", "Second message");
            bool result3 = manager.RegisterKeybind(keybind, template2);
            Assert(result3 == true, "Should allow re-registration after unregistration");

            // Test 2.4: Null keybind
            try
            {
                manager.UnregisterKeybind(null!);
                Assert(false, "Should throw ArgumentNullException for null keybind");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }

            Console.WriteLine("✓ Unregistration tests passed\n");
        }

        #endregion

        #region Availability Tests

        private static void TestAvailability()
        {
            Console.WriteLine("Test 3: Keybind Availability");
            Console.WriteLine("-----------------------------");

            using var manager = new KeybindManager();

            // Test 3.1: Unregistered keybind is available
            var keybind = CreateKeybind(ModifierKeys.Control, Key.F1);
            bool result1 = manager.IsKeybindAvailable(keybind);
            Assert(result1 == true, "Unregistered keybind should be available");

            // Test 3.2: Registered keybind is not available
            var template = CreateTemplate("Test", "Test message");
            manager.RegisterKeybind(keybind, template);
            bool result2 = manager.IsKeybindAvailable(keybind);
            Assert(result2 == false, "Registered keybind should not be available");

            // Test 3.3: Availability after unregistration
            manager.UnregisterKeybind(keybind);
            bool result3 = manager.IsKeybindAvailable(keybind);
            Assert(result3 == true, "Keybind should be available after unregistration");

            // Test 3.4: Null keybind
            try
            {
                manager.IsKeybindAvailable(null!);
                Assert(false, "Should throw ArgumentNullException for null keybind");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }

            Console.WriteLine("✓ Availability tests passed\n");
        }

        #endregion

        #region Keybind Uniqueness Tests

        private static void TestKeybindUniqueness()
        {
            Console.WriteLine("Test 4: Keybind Uniqueness");
            Console.WriteLine("--------------------------");

            using var manager = new KeybindManager();

            // Test 4.1: Different modifiers, same key
            var keybind1 = CreateKeybind(ModifierKeys.Control, Key.F1);
            var keybind2 = CreateKeybind(ModifierKeys.Shift, Key.F1);
            var template1 = CreateTemplate("Message 1", "First");
            var template2 = CreateTemplate("Message 2", "Second");
            bool result1 = manager.RegisterKeybind(keybind1, template1);
            bool result2 = manager.RegisterKeybind(keybind2, template2);
            Assert(result1 == true && result2 == true, "Different modifiers with same key should both register");

            // Test 4.2: Same modifiers, different keys
            var keybind3 = CreateKeybind(ModifierKeys.Control, Key.F2);
            var template3 = CreateTemplate("Message 3", "Third");
            bool result3 = manager.RegisterKeybind(keybind3, template3);
            Assert(result3 == true, "Same modifiers with different key should register");

            // Test 4.3: Exact duplicate
            var keybind4 = CreateKeybind(ModifierKeys.Control | ModifierKeys.Shift, Key.F1);
            var keybind5 = CreateKeybind(ModifierKeys.Control | ModifierKeys.Shift, Key.F1);
            var template4 = CreateTemplate("Message 4", "Fourth");
            var template5 = CreateTemplate("Message 5", "Fifth");
            bool result4 = manager.RegisterKeybind(keybind4, template4);
            bool result5 = manager.RegisterKeybind(keybind5, template5);
            Assert(result4 == true && result5 == false, "Exact duplicate should fail");

            Console.WriteLine("✓ Uniqueness tests passed\n");
        }

        #endregion

        #region GetRegisteredKeybinds Tests

        private static void TestGetRegisteredKeybinds()
        {
            Console.WriteLine("Test 5: Get Registered Keybinds");
            Console.WriteLine("--------------------------------");

            using var manager = new KeybindManager();

            // Test 5.1: Empty dictionary
            var keybinds1 = manager.GetRegisteredKeybinds();
            Assert(keybinds1.Count == 0, "Should return empty dictionary initially");

            // Test 5.2: Multiple keybinds
            var keybind1 = CreateKeybind(ModifierKeys.Control, Key.F1);
            var keybind2 = CreateKeybind(ModifierKeys.Shift, Key.F2);
            var template1 = CreateTemplate("Message 1", "First");
            var template2 = CreateTemplate("Message 2", "Second");
            manager.RegisterKeybind(keybind1, template1);
            manager.RegisterKeybind(keybind2, template2);
            var keybinds2 = manager.GetRegisteredKeybinds();
            Assert(keybinds2.Count == 2, "Should return all registered keybinds");

            // Test 5.3: After unregistration
            manager.UnregisterKeybind(keybind1);
            var keybinds3 = manager.GetRegisteredKeybinds();
            Assert(keybinds3.Count == 1, "Should return updated list after unregistration");

            Console.WriteLine("✓ Get registered keybinds tests passed\n");
        }

        #endregion

        #region ClearAllKeybinds Tests

        private static void TestClearAllKeybinds()
        {
            Console.WriteLine("Test 6: Clear All Keybinds");
            Console.WriteLine("--------------------------");

            using var manager = new KeybindManager();

            // Test 6.1: Clear multiple keybinds
            var keybind1 = CreateKeybind(ModifierKeys.Control, Key.F1);
            var keybind2 = CreateKeybind(ModifierKeys.Shift, Key.F2);
            var template1 = CreateTemplate("Message 1", "First");
            var template2 = CreateTemplate("Message 2", "Second");
            manager.RegisterKeybind(keybind1, template1);
            manager.RegisterKeybind(keybind2, template2);
            manager.ClearAllKeybinds();
            Assert(manager.GetRegisteredKeybinds().Count == 0, "Should remove all keybinds");

            // Test 6.2: Re-registration after clear
            var template3 = CreateTemplate("Message 3", "Third");
            bool result = manager.RegisterKeybind(keybind1, template3);
            Assert(result == true, "Should allow registration after clear");

            Console.WriteLine("✓ Clear all keybinds tests passed\n");
        }

        #endregion

        #region Event Tests

        private static void TestEventSubscription()
        {
            Console.WriteLine("Test 7: Event Subscription");
            Console.WriteLine("---------------------------");

            using var manager = new KeybindManager();
            bool eventFired = false;
            MessageTemplate? receivedTemplate = null;

            manager.KeybindPressed += (sender, args) =>
            {
                eventFired = true;
                receivedTemplate = args.Template;
            };

            // We can't easily trigger the hook in unit tests, but we can verify subscription works
            Assert(eventFired == false, "Event should not fire without actual key press");

            Console.WriteLine("✓ Event subscription test passed");
            Console.WriteLine("  (Note: Actual event firing requires integration testing with keyboard input)\n");
        }

        #endregion

        #region Hook Lifecycle Tests

        private static void TestHookLifecycle()
        {
            Console.WriteLine("Test 8: Hook Lifecycle");
            Console.WriteLine("----------------------");

            using var manager = new KeybindManager();

            // Test 8.1: Multiple StartHook calls
            manager.StartHook();
            manager.StartHook(); // Should not throw
            Assert(true, "Multiple StartHook calls should not throw");

            // Test 8.2: Multiple StopHook calls
            manager.StopHook();
            manager.StopHook(); // Should not throw
            Assert(true, "Multiple StopHook calls should not throw");

            // Test 8.3: Dispose stops hook and clears keybinds
            var keybind = CreateKeybind(ModifierKeys.Control, Key.F1);
            var template = CreateTemplate("Test", "Test message");
            manager.RegisterKeybind(keybind, template);
            manager.StartHook();
            manager.Dispose();
            Assert(manager.GetRegisteredKeybinds().Count == 0, "Dispose should clear keybinds");

            Console.WriteLine("✓ Hook lifecycle tests passed\n");
        }

        #endregion

        #region KeybindRegistration Tests

        private static void TestKeybindRegistrationClass()
        {
            Console.WriteLine("Test 9: KeybindRegistration Class");
            Console.WriteLine("----------------------------------");

            // Test 9.1: Valid construction
            var keybind = CreateKeybind(ModifierKeys.Control, Key.F1);
            var template = CreateTemplate("Test", "Test message");
            var registration = new KeybindRegistration(keybind, template);
            Assert(registration.Keybind == keybind, "Keybind should match");
            Assert(registration.Template == template, "Template should match");

            // Test 9.2: Null keybind
            try
            {
                new KeybindRegistration(null!, template);
                Assert(false, "Should throw ArgumentNullException for null keybind");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }

            // Test 9.3: Null template
            try
            {
                new KeybindRegistration(keybind, null!);
                Assert(false, "Should throw ArgumentNullException for null template");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }

            Console.WriteLine("✓ KeybindRegistration class tests passed\n");
        }

        #endregion

        #region KeybindPressedEventArgs Tests

        private static void TestKeybindPressedEventArgs()
        {
            Console.WriteLine("Test 10: KeybindPressedEventArgs Class");
            Console.WriteLine("---------------------------------------");

            // Test 10.1: Valid construction
            var template = CreateTemplate("Test", "Test message");
            var eventArgs = new KeybindPressedEventArgs(template);
            Assert(eventArgs.Template == template, "Template should match");

            // Test 10.2: Null template
            try
            {
                new KeybindPressedEventArgs(null!);
                Assert(false, "Should throw ArgumentNullException for null template");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }

            Console.WriteLine("✓ KeybindPressedEventArgs class tests passed\n");
        }

        #endregion
    }
}
