using System.Windows;
using FiveMPoliceOverlay.Services;

namespace FiveMPoliceOverlay
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
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
            
            // Application initialization will be implemented in later tasks
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            
            // Cleanup will be implemented in later tasks
        }
    }
}
