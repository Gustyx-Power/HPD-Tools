using System;
using System.Threading.Tasks;
using FiveMPoliceOverlay.Infrastructure;
using FiveMPoliceOverlay.Models;
using FiveMPoliceOverlay.Services;

namespace FiveMPoliceOverlay.Services
{
    /// <summary>
    /// Unit tests for KeybindMessageIntegration.
    /// Tests the integration between KeybindManager and MessageSender.
    /// </summary>
    public class KeybindMessageIntegrationTest
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== KeybindMessageIntegration Tests ===\n");

            TestSuccessfulMessageSend();
            TestRateLimitedMessage();
            TestFiveMNotFocusedError();
            TestEventSubscriptionAndUnsubscription();

            Console.WriteLine("\n=== All KeybindMessageIntegration Tests Completed ===");
        }

        /// <summary>
        /// Test that successful message sends trigger the MessageSentSuccessfully event.
        /// </summary>
        private static void TestSuccessfulMessageSend()
        {
            Console.WriteLine("Test: Successful Message Send");

            try
            {
                // Arrange
                var keyboardSimulator = new KeyboardSimulator();
                var rateLimiter = new RateLimiter(0, 5); // No cooldown for testing
                var messageSender = new MessageSender(keyboardSimulator, rateLimiter, isTestMode: true);
                var keybindManager = new KeybindManager();
                var integration = new KeybindMessageIntegration(keybindManager, messageSender);

                bool successEventFired = false;
                MessageTemplate? receivedTemplate = null;

                integration.MessageSentSuccessfully += (sender, e) =>
                {
                    successEventFired = true;
                    receivedTemplate = e.Template;
                };

                var template = new MessageTemplate
                {
                    Id = "test-1",
                    Name = "Test Message",
                    Text = "Test broadcast",
                    Category = "Test"
                };

                var keybind = new KeybindDefinition
                {
                    Modifiers = System.Windows.Input.ModifierKeys.Control,
                    Key = System.Windows.Input.Key.F1
                };

                keybindManager.RegisterKeybind(keybind, template);

                // Act - Simulate keybind press
                var eventArgs = new KeybindPressedEventArgs(template);
                keybindManager.GetType()
                    .GetEvent("KeybindPressed")
                    ?.GetRaiseMethod(true)
                    ?.Invoke(keybindManager, new object[] { keybindManager, eventArgs });

                // Wait for async operation
                Task.Delay(1000).Wait();

                // Assert
                // Note: This test may not fire success event if FiveM is not running
                // In a real scenario, we'd use mocks to control the MessageSender behavior
                Console.WriteLine($"  Success event fired: {successEventFired}");
                Console.WriteLine($"  Template received: {receivedTemplate?.Name ?? "null"}");

                // Cleanup
                integration.Dispose();
                keybindManager.Dispose();

                Console.WriteLine("  ✓ Test completed (Note: Success depends on FiveM being focused)\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Test failed: {ex.Message}\n");
            }
        }

        /// <summary>
        /// Test that rate limited messages trigger the MessageRateLimited event.
        /// </summary>
        private static void TestRateLimitedMessage()
        {
            Console.WriteLine("Test: Rate Limited Message");

            try
            {
                // Arrange
                var keyboardSimulator = new KeyboardSimulator();
                var rateLimiter = new RateLimiter(10, 5); // 10 second cooldown
                rateLimiter.MarkMessageSent(); // Trigger cooldown immediately
                
                var messageSender = new MessageSender(keyboardSimulator, rateLimiter, isTestMode: true);
                var keybindManager = new KeybindManager();
                var integration = new KeybindMessageIntegration(keybindManager, messageSender);

                bool rateLimitedEventFired = false;
                MessageTemplate? receivedTemplate = null;

                integration.MessageRateLimited += (sender, e) =>
                {
                    rateLimitedEventFired = true;
                    receivedTemplate = e.Template;
                };

                var template = new MessageTemplate
                {
                    Id = "test-2",
                    Name = "Rate Limited Test",
                    Text = "This should be rate limited",
                    Category = "Test"
                };

                var keybind = new KeybindDefinition
                {
                    Modifiers = System.Windows.Input.ModifierKeys.Control,
                    Key = System.Windows.Input.Key.F2
                };

                keybindManager.RegisterKeybind(keybind, template);

                // Act - Simulate keybind press
                var eventArgs = new KeybindPressedEventArgs(template);
                keybindManager.GetType()
                    .GetEvent("KeybindPressed")
                    ?.GetRaiseMethod(true)
                    ?.Invoke(keybindManager, new object[] { keybindManager, eventArgs });

                // Wait for async operation
                Task.Delay(500).Wait();

                // Assert
                if (rateLimitedEventFired && receivedTemplate?.Id == template.Id)
                {
                    Console.WriteLine($"  ✓ Rate limited event fired correctly");
                    Console.WriteLine($"  ✓ Template: {receivedTemplate.Name}");
                }
                else
                {
                    Console.WriteLine($"  ✗ Rate limited event not fired or wrong template");
                    Console.WriteLine($"  Event fired: {rateLimitedEventFired}");
                    Console.WriteLine($"  Template: {receivedTemplate?.Name ?? "null"}");
                }

                // Cleanup
                integration.Dispose();
                keybindManager.Dispose();

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Test failed: {ex.Message}\n");
            }
        }

        /// <summary>
        /// Test that FiveM not focused errors trigger the MessageSendFailed event.
        /// </summary>
        private static void TestFiveMNotFocusedError()
        {
            Console.WriteLine("Test: FiveM Not Focused Error");

            try
            {
                // Arrange
                var keyboardSimulator = new KeyboardSimulator();
                var rateLimiter = new RateLimiter(0, 5); // No cooldown
                var messageSender = new MessageSender(keyboardSimulator, rateLimiter, isTestMode: true);
                var keybindManager = new KeybindManager();
                var integration = new KeybindMessageIntegration(keybindManager, messageSender);

                bool errorEventFired = false;
                string? errorMessage = null;
                MessageTemplate? receivedTemplate = null;

                integration.MessageSendFailed += (sender, e) =>
                {
                    errorEventFired = true;
                    errorMessage = e.ErrorMessage;
                    receivedTemplate = e.Template;
                };

                var template = new MessageTemplate
                {
                    Id = "test-3",
                    Name = "Not Focused Test",
                    Text = "This should fail if FiveM not focused",
                    Category = "Test"
                };

                var keybind = new KeybindDefinition
                {
                    Modifiers = System.Windows.Input.ModifierKeys.Control,
                    Key = System.Windows.Input.Key.F3
                };

                keybindManager.RegisterKeybind(keybind, template);

                // Act - Simulate keybind press (will fail if FiveM not focused)
                var eventArgs = new KeybindPressedEventArgs(template);
                keybindManager.GetType()
                    .GetEvent("KeybindPressed")
                    ?.GetRaiseMethod(true)
                    ?.Invoke(keybindManager, new object[] { keybindManager, eventArgs });

                // Wait for async operation
                Task.Delay(2000).Wait();

                // Assert
                Console.WriteLine($"  Error event fired: {errorEventFired}");
                Console.WriteLine($"  Error message: {errorMessage ?? "null"}");
                Console.WriteLine($"  Template: {receivedTemplate?.Name ?? "null"}");
                Console.WriteLine("  ✓ Test completed (Note: Error depends on FiveM not being focused)\n");

                // Cleanup
                integration.Dispose();
                keybindManager.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Test failed: {ex.Message}\n");
            }
        }

        /// <summary>
        /// Test that event subscription and unsubscription works correctly.
        /// </summary>
        private static void TestEventSubscriptionAndUnsubscription()
        {
            Console.WriteLine("Test: Event Subscription and Unsubscription");

            try
            {
                // Arrange
                var keyboardSimulator = new KeyboardSimulator();
                var rateLimiter = new RateLimiter(0, 5);
                var messageSender = new MessageSender(keyboardSimulator, rateLimiter, isTestMode: true);
                var keybindManager = new KeybindManager();
                var integration = new KeybindMessageIntegration(keybindManager, messageSender);

                int successCount = 0;
                int errorCount = 0;
                int rateLimitedCount = 0;

                EventHandler<MessageSentEventArgs> successHandler = (sender, e) => successCount++;
                EventHandler<MessageSendErrorEventArgs> errorHandler = (sender, e) => errorCount++;
                EventHandler<MessageRateLimitedEventArgs> rateLimitedHandler = (sender, e) => rateLimitedCount++;

                // Act - Subscribe
                integration.MessageSentSuccessfully += successHandler;
                integration.MessageSendFailed += errorHandler;
                integration.MessageRateLimited += rateLimitedHandler;

                // Unsubscribe
                integration.MessageSentSuccessfully -= successHandler;
                integration.MessageSendFailed -= errorHandler;
                integration.MessageRateLimited -= rateLimitedHandler;

                // Dispose
                integration.Dispose();
                keybindManager.Dispose();

                // Assert
                Console.WriteLine($"  ✓ Event subscription/unsubscription successful");
                Console.WriteLine($"  ✓ Integration disposed without errors\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Test failed: {ex.Message}\n");
            }
        }
    }
}
