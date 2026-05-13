using System;
using FiveMPoliceOverlay.Services;

namespace FiveMPoliceOverlay
{
    /// <summary>
    /// Simple test runner for manual tests
    /// </summary>
    class TestRunner
    {
        static void Main(string[] args)
        {
            Console.WriteLine("FiveM Police Overlay - Test Runner");
            Console.WriteLine("===================================\n");

            if (args.Length > 0 && args[0] == "ratelimiter")
            {
                RateLimiterTest.RunTests();
            }
            else if (args.Length > 0 && args[0] == "processmonitor")
            {
                ProcessMonitorTest.RunTests();
            }
            else if (args.Length > 0 && args[0] == "keybindmanager")
            {
                KeybindManagerTest.RunTests();
            }
            else
            {
                Console.WriteLine("Usage: TestRunner.exe [test-name]");
                Console.WriteLine("\nAvailable tests:");
                Console.WriteLine("  ratelimiter     - Run RateLimiter tests");
                Console.WriteLine("  processmonitor  - Run ProcessMonitor tests");
                Console.WriteLine("  keybindmanager  - Run KeybindManager tests");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
