using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using FiveMPoliceOverlay.Infrastructure;
using FiveMPoliceOverlay.Models;
using FiveMPoliceOverlay.Services;
using FiveMPoliceOverlay.Views;

namespace FiveMPoliceOverlay
{
    /// <summary>
    /// Application entry point. Manages full lifecycle:
    /// Initialize services → Start in system tray → Monitor FiveM → Graceful shutdown.
    /// </summary>
    public partial class App : Application
    {
        #region Fields

        private ProcessMonitor? _processMonitor;
        private OverlayManager? _overlayManager;
        private ConfigurationManager? _configManager;
        private KeybindManager? _keybindManager;
        private SystemTrayApp? _systemTray;
        private MessageSender? _messageSender;
        private KeybindMessageIntegration? _integration;

        #endregion

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Global exception handlers
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // Test mode entry points
            if (e.Args.Length > 0)
            {
                if (HandleTestArgs(e.Args[0])) return;
            }

            // Production initialization
            try
            {
                InitializeApplication();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[App] CRITICAL: Initialization failed: {ex}");
                MessageBox.Show(
                    $"Gagal menginisialisasi aplikasi:\n\n{ex.Message}\n\nAplikasi akan ditutup.",
                    "Error Kritis - HOPE PD SkyNews",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        /// <summary>
        /// Full application initialization sequence.
        /// </summary>
        private void InitializeApplication()
        {
            Console.WriteLine("[App] Initializing HOPE PD SkyNews...");

            // 1. Configuration
            _configManager = new ConfigurationManager();
            var config = _configManager.LoadConfiguration();
            Console.WriteLine("[App] Configuration loaded");

            // 2. Keybind Manager + register keybinds from config
            _keybindManager = new KeybindManager();

            // 3. Overlay Manager
            _overlayManager = new OverlayManager(_configManager, _keybindManager);
            _overlayManager.RegisterToggleKeybind();

            // 4. Message Sender
            var keyboardSim = new KeyboardSimulator();
            var rateLimiter = new RateLimiter(
                config.RateLimiting.CooldownSeconds, config.RateLimiting.MaxQueueSize);
            _messageSender = new MessageSender(keyboardSim, rateLimiter, config.General.TestMode);

            // 5. Integration: wire keybind presses to message sending
            _integration = new KeybindMessageIntegration(_keybindManager, _messageSender);

            // Wire overlay feedback
            _integration.MessageSentSuccessfully += (s, args) =>
                Dispatcher.Invoke(() => _overlayManager.GetOverlayWindow()?.ShowSuccessFeedback());
            _integration.MessageRateLimited += (s, args) =>
                Dispatcher.Invoke(() => _overlayManager.GetOverlayWindow()?.ShowCooldown(2));

            // 6. Register configured keybinds
            RegisterConfiguredKeybinds(config);

            // 7. Start keyboard hook
            _keybindManager.StartHook();
            Console.WriteLine("[App] Keyboard hook started");

            // 8. System Tray (before ProcessMonitor so event handlers can access it)
            _systemTray = new SystemTrayApp(_overlayManager, _configManager);
            Console.WriteLine("[App] System tray initialized");

            // 9. Process Monitor
            _processMonitor = new ProcessMonitor();
            _processMonitor.FiveMStarted += async (sender, args) =>
            {
                Console.WriteLine("[App] FiveM started, waiting 5s before showing overlay...");
                _systemTray?.ShowNotification("FiveM Terdeteksi", "Overlay akan ditampilkan dalam 5 detik...");
                if (_systemTray != null) _systemTray.IsFiveMDetected = true;
                await Task.Delay(TimeSpan.FromSeconds(5));
                Dispatcher.Invoke(() => _overlayManager?.ShowOverlay());
            };
            _processMonitor.FiveMStopped += (sender, args) =>
            {
                Console.WriteLine("[App] FiveM stopped, hiding overlay");
                _systemTray?.ShowNotification("FiveM Berhenti", "Overlay disembunyikan.");
                if (_systemTray != null) _systemTray.IsFiveMDetected = false;
                Dispatcher.Invoke(() => _overlayManager?.HideOverlay());
            };
            _processMonitor.StartMonitoring();
            Console.WriteLine("[App] ProcessMonitor started");
            Console.WriteLine("[App] ✓ Initialization complete - running in system tray");
        }

        /// <summary>
        /// Registers all keybind mappings from configuration.
        /// </summary>
        private void RegisterConfiguredKeybinds(AppConfiguration config)
        {
            foreach (var mapping in config.Keybinds)
            {
                var template = config.Templates.FirstOrDefault(t => t.Id == mapping.TemplateId);
                if (template != null)
                {
                    _keybindManager!.RegisterKeybind(mapping.Keybind, template);
                    Console.WriteLine($"[App] Registered keybind: {mapping.Keybind} → {template.Name}");
                }
            }
        }

        /// <summary>
        /// Handles test command-line arguments.
        /// </summary>
        private bool HandleTestArgs(string arg)
        {
            switch (arg)
            {
                case "--test-ratelimiter":
                    RateLimiterTest.RunTests(); Shutdown(); return true;
                case "--test-integration":
                    KeybindMessageIntegrationTest.RunAllTests(); Shutdown(); return true;
                case "--test-overlay":
                    OverlayWindowTest.RunTests(); Shutdown(); return true;
                case "--test-overlaymanager":
                    OverlayManagerTest.RunTests(); Shutdown(); return true;
                case "--test-processmonitor-integration":
                    ProcessMonitorIntegrationTest.RunTests(); Shutdown(); return true;
                case "--demo-integration":
                    ProcessMonitorIntegrationDemo.RunDemo(); Shutdown(); return true;
                default:
                    return false;
            }
        }

        #region Error Handling

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Console.WriteLine($"[App] UNHANDLED UI EXCEPTION: {e.Exception}");
            e.Handled = true;

            ShowCriticalErrorDialog(e.Exception);
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                Console.WriteLine($"[App] UNHANDLED DOMAIN EXCEPTION: {ex}");
                ShowCriticalErrorDialog(ex);
            }
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Console.WriteLine($"[App] UNOBSERVED TASK EXCEPTION: {e.Exception}");
            e.SetObserved();
        }

        private void ShowCriticalErrorDialog(Exception ex)
        {
            try
            {
                var result = MessageBox.Show(
                    $"Terjadi error yang tidak terduga:\n\n{ex.Message}\n\n" +
                    "Klik 'Yes' untuk reset konfigurasi dan restart,\n" +
                    "atau 'No' untuk melanjutkan.",
                    "Error - HOPE PD SkyNews",
                    MessageBoxButton.YesNo, MessageBoxImage.Error);

                if (result == MessageBoxResult.Yes)
                {
                    _configManager?.ResetToDefaults();
                    Console.WriteLine("[App] Configuration reset after critical error");
                }
            }
            catch
            {
                // If even the error dialog fails, silently continue
            }
        }

        #endregion

        protected override void OnExit(ExitEventArgs e)
        {
            Console.WriteLine("[App] Shutting down...");

            try
            {
                // Save current configuration
                if (_configManager != null)
                {
                    var config = _configManager.LoadConfiguration();
                    _configManager.SaveConfiguration(config).Wait();
                    Console.WriteLine("[App] Configuration saved");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[App] Error saving config on exit: {ex.Message}");
            }

            // Dispose in reverse initialization order
            _systemTray?.Dispose();
            _processMonitor?.Dispose();
            _integration = null;
            _overlayManager?.Dispose();
            _keybindManager?.Dispose();

            Console.WriteLine("[App] Cleanup complete");
            base.OnExit(e);
        }
    }
}
