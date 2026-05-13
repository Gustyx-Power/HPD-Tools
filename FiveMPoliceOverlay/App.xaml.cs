using System;
using System.Threading.Tasks;
using System.Windows;
using FiveMPoliceOverlay.Services;
using FiveMPoliceOverlay.Views;

namespace FiveMPoliceOverlay
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ProcessMonitor? _processMonitor;
        private OverlayManager? _overlayManager;
        private ConfigurationManager? _configManager;
        private KeybindManager? _keybindManager;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Check if running in test mode
            if (e.Args.Length > 0 && e.Args[0] == "--test-ratelimiter")
            {
                RateLimiterTest.RunTests();
                Shutdown();
                return;
            }
            
            if (e.Args.Length > 0 && e.Args[0] == "--test-integration")
            {
                KeybindMessageIntegrationTest.RunAllTests();
                Shutdown();
                return;
            }
            
            if (e.Args.Length > 0 && e.Args[0] == "--test-overlay")
            {
                OverlayWindowTest.RunTests();
                Shutdown();
                return;
            }
            
            if (e.Args.Length > 0 && e.Args[0] == "--test-overlaymanager")
            {
                OverlayManagerTest.RunTests();
                Shutdown();
                return;
            }
            
            if (e.Args.Length > 0 && e.Args[0] == "--test-processmonitor-integration")
            {
                ProcessMonitorIntegrationTest.RunTests();
                Shutdown();
                return;
            }
            
            if (e.Args.Length > 0 && e.Args[0] == "--demo-integration")
            {
                ProcessMonitorIntegrationDemo.RunDemo();
                Shutdown();
                return;
            }
            
            // Application initialization will be implemented in later tasks
        }

        /// <summary>
        /// Initializes the ProcessMonitor and OverlayManager integration.
        /// This method wires the FiveMStarted event to ShowOverlay and FiveMStopped event to HideOverlay.
        /// </summary>
        private void InitializeProcessMonitorIntegration()
        {
            // Initialize services
            _configManager = new ConfigurationManager();
            _keybindManager = new KeybindManager();
            _overlayManager = new OverlayManager(_configManager, _keybindManager);
            _processMonitor = new ProcessMonitor();

            // Wire FiveMStarted event to ShowOverlay with 5-second timeout
            _processMonitor.FiveMStarted += async (sender, args) =>
            {
                Console.WriteLine("[App] FiveM started event received, waiting 5 seconds before showing overlay...");
                
                // Wait 5 seconds to allow FiveM to fully start
                await Task.Delay(TimeSpan.FromSeconds(5));
                
                Console.WriteLine("[App] 5-second timeout complete, showing overlay...");
                _overlayManager.ShowOverlay();
            };

            // Wire FiveMStopped event to HideOverlay
            _processMonitor.FiveMStopped += (sender, args) =>
            {
                Console.WriteLine("[App] FiveM stopped event received, hiding overlay...");
                _overlayManager.HideOverlay();
            };

            // Start monitoring for FiveM process
            _processMonitor.StartMonitoring();
            Console.WriteLine("[App] ProcessMonitor integration initialized and monitoring started");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            
            // Cleanup resources
            _processMonitor?.Dispose();
            _overlayManager?.Dispose();
            _keybindManager?.Dispose();
            
            Console.WriteLine("[App] Application cleanup complete");
        }
    }
}
