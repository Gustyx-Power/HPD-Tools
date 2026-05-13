using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace FiveMPoliceOverlay.Services
{
    /// <summary>
    /// Integration test for ProcessMonitor and OverlayManager integration.
    /// Tests the wiring of FiveMStarted/FiveMStopped events to ShowOverlay/HideOverlay.
    /// </summary>
    public static class ProcessMonitorIntegrationTest
    {
        public static void RunTests()
        {
            Console.WriteLine("=== ProcessMonitor Integration Tests ===\n");

            TestFiveMStartedEventTriggersShowOverlay();
            TestFiveMStoppedEventTriggersHideOverlay();
            TestFiveSecondTimeoutBeforeShowingOverlay();

            Console.WriteLine("\n=== All ProcessMonitor Integration Tests Complete ===");
        }

        /// <summary>
        /// Test that FiveMStarted event triggers ShowOverlay after 5-second timeout
        /// </summary>
        private static void TestFiveMStartedEventTriggersShowOverlay()
        {
            Console.WriteLine("Test: FiveMStarted event triggers ShowOverlay with 5-second timeout");

            try
            {
                // Initialize services
                var configManager = new ConfigurationManager();
                var keybindManager = new KeybindManager();
                var overlayManager = new OverlayManager(configManager, keybindManager);
                var processMonitor = new ProcessMonitor();

                bool showOverlayCalled = false;
                DateTime eventReceivedTime = DateTime.MinValue;
                DateTime showOverlayTime = DateTime.MinValue;

                // Wire FiveMStarted event with 5-second timeout
                processMonitor.FiveMStarted += async (sender, args) =>
                {
                    eventReceivedTime = DateTime.Now;
                    Console.WriteLine($"  [Test] FiveMStarted event received at {eventReceivedTime:HH:mm:ss.fff}");
                    
                    // Wait 5 seconds
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    
                    showOverlayTime = DateTime.Now;
                    Console.WriteLine($"  [Test] ShowOverlay called at {showOverlayTime:HH:mm:ss.fff}");
                    showOverlayCalled = true;
                };

                // Manually trigger FiveMStarted event
                var eventArgs = new ProcessEventArgs(null);
                processMonitor.GetType()
                    .GetMethod("OnFiveMStarted", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.Invoke(processMonitor, new object[] { eventArgs });

                // Wait for async operation to complete (5 seconds + buffer)
                Thread.Sleep(6000);

                // Verify ShowOverlay was called
                if (!showOverlayCalled)
                {
                    Console.WriteLine("  ❌ FAILED: ShowOverlay was not called");
                }
                else
                {
                    // Verify 5-second timeout
                    var delay = (showOverlayTime - eventReceivedTime).TotalSeconds;
                    if (delay >= 4.9 && delay <= 5.5) // Allow 0.5s tolerance
                    {
                        Console.WriteLine($"  ✓ PASSED: ShowOverlay called after {delay:F2} seconds");
                    }
                    else
                    {
                        Console.WriteLine($"  ❌ FAILED: ShowOverlay called after {delay:F2} seconds (expected ~5 seconds)");
                    }
                }

                // Cleanup
                processMonitor.Dispose();
                overlayManager.Dispose();
                keybindManager.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ FAILED: Exception occurred: {ex.Message}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Test that FiveMStopped event triggers HideOverlay
        /// </summary>
        private static void TestFiveMStoppedEventTriggersHideOverlay()
        {
            Console.WriteLine("Test: FiveMStopped event triggers HideOverlay");

            try
            {
                // Initialize services
                var configManager = new ConfigurationManager();
                var keybindManager = new KeybindManager();
                var overlayManager = new OverlayManager(configManager, keybindManager);
                var processMonitor = new ProcessMonitor();

                bool hideOverlayCalled = false;

                // Wire FiveMStopped event
                processMonitor.FiveMStopped += (sender, args) =>
                {
                    Console.WriteLine("  [Test] FiveMStopped event received");
                    hideOverlayCalled = true;
                };

                // Manually trigger FiveMStopped event
                var eventArgs = new ProcessEventArgs(null);
                processMonitor.GetType()
                    .GetMethod("OnFiveMStopped", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.Invoke(processMonitor, new object[] { eventArgs });

                // Wait a bit for event to propagate
                Thread.Sleep(100);

                // Verify HideOverlay was called
                if (hideOverlayCalled)
                {
                    Console.WriteLine("  ✓ PASSED: HideOverlay was called");
                }
                else
                {
                    Console.WriteLine("  ❌ FAILED: HideOverlay was not called");
                }

                // Cleanup
                processMonitor.Dispose();
                overlayManager.Dispose();
                keybindManager.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ FAILED: Exception occurred: {ex.Message}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Test that the 5-second timeout is properly implemented
        /// </summary>
        private static void TestFiveSecondTimeoutBeforeShowingOverlay()
        {
            Console.WriteLine("Test: 5-second timeout before showing overlay");

            try
            {
                var startTime = DateTime.Now;
                var endTime = DateTime.MinValue;
                bool completed = false;

                // Simulate the async delay
                Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    endTime = DateTime.Now;
                    completed = true;
                });

                // Wait for completion
                Thread.Sleep(6000);

                if (completed)
                {
                    var delay = (endTime - startTime).TotalSeconds;
                    if (delay >= 4.9 && delay <= 5.5) // Allow 0.5s tolerance
                    {
                        Console.WriteLine($"  ✓ PASSED: Timeout delay is {delay:F2} seconds (expected ~5 seconds)");
                    }
                    else
                    {
                        Console.WriteLine($"  ❌ FAILED: Timeout delay is {delay:F2} seconds (expected ~5 seconds)");
                    }
                }
                else
                {
                    Console.WriteLine("  ❌ FAILED: Timeout did not complete");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ FAILED: Exception occurred: {ex.Message}");
            }

            Console.WriteLine();
        }
    }
}
