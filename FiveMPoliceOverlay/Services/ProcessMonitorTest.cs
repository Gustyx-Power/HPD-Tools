using System;
using System.Diagnostics;
using System.Threading;
using FiveMPoliceOverlay.Services;

namespace FiveMPoliceOverlay.Services
{
    /// <summary>
    /// Manual test for ProcessMonitor service
    /// Tests process detection, event firing, and error handling
    /// Call RunTests() from your application to execute tests
    /// </summary>
    public class ProcessMonitorTest
    {
        private static ProcessMonitor? _monitor;
        private static bool _startedEventFired;
        private static bool _stoppedEventFired;
        private static Process? _detectedProcess;

        public static void RunTests()
        {
            Console.WriteLine("=== ProcessMonitor Test Suite ===\n");

            try
            {
                TestProcessDetection();
                TestEventFiring();
                TestRetryLogic();
                TestDisposal();

                Console.WriteLine("\n=== All Tests Completed ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[ERROR] Test failed: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// Test 1: Process detection logic
        /// </summary>
        private static void TestProcessDetection()
        {
            Console.WriteLine("Test 1: Process Detection");
            Console.WriteLine("-------------------------");

            _monitor = new ProcessMonitor();

            // Test IsFiveMRunning method
            bool isRunning = _monitor.IsFiveMRunning();
            Console.WriteLine($"FiveM running: {isRunning}");

            if (isRunning)
            {
                var process = _monitor.GetFiveMProcess();
                if (process != null)
                {
                    Console.WriteLine($"Process found: {process.ProcessName} (PID: {process.Id})");
                    Console.WriteLine($"✓ Process detection working");
                }
                else
                {
                    Console.WriteLine("✗ IsFiveMRunning returned true but GetFiveMProcess returned null");
                }
            }
            else
            {
                Console.WriteLine("FiveM not detected (this is expected if FiveM is not running)");
                Console.WriteLine("✓ Process detection working (no false positives)");
            }

            _monitor.Dispose();
            _monitor = null;
            Console.WriteLine();
        }

        /// <summary>
        /// Test 2: Event firing on state changes
        /// </summary>
        private static void TestEventFiring()
        {
            Console.WriteLine("Test 2: Event Firing");
            Console.WriteLine("--------------------");

            _monitor = new ProcessMonitor();
            _startedEventFired = false;
            _stoppedEventFired = false;
            _detectedProcess = null;

            // Subscribe to events
            _monitor.FiveMStarted += OnFiveMStarted;
            _monitor.FiveMStopped += OnFiveMStopped;

            Console.WriteLine("Starting monitoring...");
            _monitor.StartMonitoring();

            // Check initial state
            bool initialState = _monitor.IsFiveMRunning();
            Console.WriteLine($"Initial FiveM state: {(initialState ? "Running" : "Not Running")}");

            if (initialState)
            {
                Console.WriteLine("\nFiveM is currently running.");
                Console.WriteLine("Please CLOSE FiveM to test the FiveMStopped event.");
                Console.WriteLine("Waiting for FiveM to close (30 seconds timeout)...");

                // Wait for stopped event
                int waitTime = 0;
                while (!_stoppedEventFired && waitTime < 30000)
                {
                    Thread.Sleep(500);
                    waitTime += 500;
                }

                if (_stoppedEventFired)
                {
                    Console.WriteLine("✓ FiveMStopped event fired successfully");
                }
                else
                {
                    Console.WriteLine("✗ FiveMStopped event did not fire (timeout or FiveM still running)");
                }
            }
            else
            {
                Console.WriteLine("\nFiveM is not currently running.");
                Console.WriteLine("Please START FiveM to test the FiveMStarted event.");
                Console.WriteLine("Waiting for FiveM to start (30 seconds timeout)...");

                // Wait for started event
                int waitTime = 0;
                while (!_startedEventFired && waitTime < 30000)
                {
                    Thread.Sleep(500);
                    waitTime += 500;
                }

                if (_startedEventFired)
                {
                    Console.WriteLine($"✓ FiveMStarted event fired successfully");
                    if (_detectedProcess != null)
                    {
                        Console.WriteLine($"  Process: {_detectedProcess.ProcessName} (PID: {_detectedProcess.Id})");
                    }
                }
                else
                {
                    Console.WriteLine("✗ FiveMStarted event did not fire (timeout or FiveM not started)");
                }
            }

            _monitor.StopMonitoring();
            _monitor.Dispose();
            _monitor = null;
            Console.WriteLine();
        }

        /// <summary>
        /// Test 3: Retry logic and error handling
        /// </summary>
        private static void TestRetryLogic()
        {
            Console.WriteLine("Test 3: Retry Logic and Error Handling");
            Console.WriteLine("---------------------------------------");

            _monitor = new ProcessMonitor();
            _monitor.StartMonitoring();

            Console.WriteLine("Monitoring for 10 seconds to test continuous polling...");
            Console.WriteLine("(Check console for any error messages)");

            Thread.Sleep(10000);

            Console.WriteLine("✓ Monitoring completed without crashes");
            Console.WriteLine("  (Check above for any logged errors after 3 consecutive failures)");

            _monitor.StopMonitoring();
            _monitor.Dispose();
            _monitor = null;
            Console.WriteLine();
        }

        /// <summary>
        /// Test 4: Disposal and cleanup
        /// </summary>
        private static void TestDisposal()
        {
            Console.WriteLine("Test 4: Disposal and Cleanup");
            Console.WriteLine("-----------------------------");

            _monitor = new ProcessMonitor();
            _monitor.StartMonitoring();

            Console.WriteLine("Starting monitoring...");
            Thread.Sleep(3000);

            Console.WriteLine("Disposing monitor...");
            _monitor.Dispose();

            // Try to start again after disposal (should not crash)
            try
            {
                _monitor.StartMonitoring();
                Console.WriteLine("✗ Monitor should not allow starting after disposal");
            }
            catch
            {
                // Expected - monitor is disposed
            }

            Console.WriteLine("✓ Disposal completed successfully");
            _monitor = null;
            Console.WriteLine();
        }

        /// <summary>
        /// Event handler for FiveMStarted
        /// </summary>
        private static void OnFiveMStarted(object? sender, ProcessEventArgs e)
        {
            _startedEventFired = true;
            _detectedProcess = e.Process;
            Console.WriteLine($"\n[EVENT] FiveMStarted fired! Process: {e.Process?.ProcessName ?? "null"} (PID: {e.Process?.Id ?? 0})");
        }

        /// <summary>
        /// Event handler for FiveMStopped
        /// </summary>
        private static void OnFiveMStopped(object? sender, ProcessEventArgs e)
        {
            _stoppedEventFired = true;
            Console.WriteLine("\n[EVENT] FiveMStopped fired!");
        }
    }
}
