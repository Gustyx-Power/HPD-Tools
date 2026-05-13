using System;
using System.Windows;
using FiveMPoliceOverlay.Models;
using FiveMPoliceOverlay.Views;

namespace FiveMPoliceOverlay.Services
{
    /// <summary>
    /// Manages the overlay window lifecycle and visibility.
    /// Handles overlay creation, disposal, visibility toggling, and position persistence.
    /// </summary>
    public class OverlayManager : IDisposable
    {
        #region Fields

        private OverlayWindow? _overlayWindow;
        private readonly ConfigurationManager _configManager;
        private readonly KeybindManager _keybindManager;
        private bool _isVisible;
        private bool _isDisposed;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new OverlayManager with required dependencies.
        /// </summary>
        /// <param name="configManager">Configuration manager for loading/saving settings</param>
        /// <param name="keybindManager">Keybind manager for registering toggle keybind</param>
        public OverlayManager(ConfigurationManager configManager, KeybindManager keybindManager)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _keybindManager = keybindManager ?? throw new ArgumentNullException(nameof(keybindManager));
            _isVisible = false;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the overlay window. Creates the window if it doesn't exist.
        /// Loads position from configuration and subscribes to position changes.
        /// </summary>
        public void ShowOverlay()
        {
            if (_isDisposed)
            {
                Console.WriteLine("[OverlayManager] Cannot show overlay: manager is disposed");
                return;
            }

            // Create overlay window if it doesn't exist
            if (_overlayWindow == null)
            {
                CreateOverlayWindow();
            }

            // Show the overlay window
            if (_overlayWindow != null && !_isVisible)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _overlayWindow.Show();
                    _isVisible = true;
                    Console.WriteLine("[OverlayManager] Overlay shown");

                    // Save visibility state to configuration
                    SaveVisibilityState(true);
                });
            }
        }

        /// <summary>
        /// Hides the overlay window without disposing it.
        /// </summary>
        public void HideOverlay()
        {
            if (_overlayWindow != null && _isVisible)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _overlayWindow.Hide();
                    _isVisible = false;
                    Console.WriteLine("[OverlayManager] Overlay hidden");

                    // Save visibility state to configuration
                    SaveVisibilityState(false);
                });
            }
        }

        /// <summary>
        /// Toggles overlay panel between expanded and collapsed (ReShade-style).
        /// </summary>
        public void ToggleOverlay()
        {
            if (_overlayWindow == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                _overlayWindow.TogglePanel();
                Console.WriteLine($"[OverlayManager] Panel toggled: {(_overlayWindow.IsExpanded ? "expanded" : "collapsed")}");
            });
        }

        /// <summary>
        /// Updates the overlay position.
        /// </summary>
        /// <param name="position">New position (X, Y coordinates)</param>
        public void UpdatePosition(Point position)
        {
            if (_overlayWindow != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _overlayWindow.UpdatePosition(position.X, position.Y);
                    Console.WriteLine($"[OverlayManager] Overlay position updated to ({position.X}, {position.Y})");
                });
            }
        }

        /// <summary>
        /// Registers the toggle keybind (default: Ctrl+F10) to toggle overlay visibility.
        /// </summary>
        public void RegisterToggleKeybind()
        {
            var config = _configManager.LoadConfiguration();
            var toggleKeybind = config.Overlay.ToggleKeybind;

            // Create a dummy template for the toggle keybind
            var toggleTemplate = new MessageTemplate
            {
                Id = "toggle-overlay",
                Category = "System",
                Name = "Toggle Overlay",
                Text = "", // No message text needed for toggle
                IsPredefined = true
            };

            // Register the toggle keybind
            bool registered = _keybindManager.RegisterKeybind(toggleKeybind, toggleTemplate);
            
            if (registered)
            {
                Console.WriteLine($"[OverlayManager] Toggle keybind registered: {toggleKeybind}");
            }
            else
            {
                Console.WriteLine($"[OverlayManager] WARNING: Failed to register toggle keybind: {toggleKeybind}");
            }

            // Subscribe to keybind pressed event
            _keybindManager.KeybindPressed += OnKeybindPressed;
        }

        /// <summary>
        /// Unregisters the toggle keybind.
        /// </summary>
        public void UnregisterToggleKeybind()
        {
            var config = _configManager.LoadConfiguration();
            var toggleKeybind = config.Overlay.ToggleKeybind;

            _keybindManager.UnregisterKeybind(toggleKeybind);
            _keybindManager.KeybindPressed -= OnKeybindPressed;
            
            Console.WriteLine("[OverlayManager] Toggle keybind unregistered");
        }

        /// <summary>
        /// Gets the current overlay window instance.
        /// </summary>
        /// <returns>The overlay window or null if not created</returns>
        public OverlayWindow? GetOverlayWindow()
        {
            return _overlayWindow;
        }

        /// <summary>
        /// Gets the current visibility state of the overlay.
        /// </summary>
        /// <returns>True if overlay is visible, false otherwise</returns>
        public bool IsVisible()
        {
            return _isVisible;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates the overlay window with position loaded from configuration.
        /// Subscribes to PositionChanged event to save position changes.
        /// </summary>
        private void CreateOverlayWindow()
        {
            var config = _configManager.LoadConfiguration();
            var position = config.Overlay.Position;

            Application.Current.Dispatcher.Invoke(() =>
            {
                _overlayWindow = new OverlayWindow(position);
                _overlayWindow.SetConfigManager(_configManager);
                _overlayWindow.SetKeybindManager(_keybindManager);
                
                // Subscribe to position changed event
                _overlayWindow.PositionChanged += OnOverlayPositionChanged;
                
                // Set initial mode (test/production)
                _overlayWindow.SetMode(config.General.TestMode);
                
                Console.WriteLine($"[OverlayManager] Overlay window created at position ({position.X}, {position.Y})");
            });
        }

        /// <summary>
        /// Handles overlay position changed event and saves to configuration.
        /// </summary>
        private void OnOverlayPositionChanged(object? sender, Point newPosition)
        {
            SavePosition(newPosition);
        }

        /// <summary>
        /// Handles keybind pressed event to toggle overlay when toggle keybind is pressed.
        /// </summary>
        private void OnKeybindPressed(object? sender, KeybindPressedEventArgs e)
        {
            // Check if this is the toggle overlay keybind
            if (e.Template.Id == "toggle-overlay")
            {
                ToggleOverlay();
            }
        }

        /// <summary>
        /// Saves the overlay position to configuration.
        /// </summary>
        private void SavePosition(Point position)
        {
            try
            {
                var config = _configManager.LoadConfiguration();
                config.Overlay.Position = position;
                _configManager.SaveConfiguration(config).Wait();
                Console.WriteLine($"[OverlayManager] Overlay position saved: ({position.X}, {position.Y})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OverlayManager] ERROR: Failed to save overlay position: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves the overlay visibility state to configuration.
        /// </summary>
        private void SaveVisibilityState(bool isVisible)
        {
            try
            {
                var config = _configManager.LoadConfiguration();
                config.Overlay.IsVisible = isVisible;
                _configManager.SaveConfiguration(config).Wait();
                Console.WriteLine($"[OverlayManager] Overlay visibility state saved: {isVisible}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OverlayManager] ERROR: Failed to save visibility state: {ex.Message}");
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes the overlay manager and cleans up resources.
        /// Closes and disposes the overlay window if it exists.
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                // Unregister toggle keybind
                UnregisterToggleKeybind();

                // Dispose overlay window
                if (_overlayWindow != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _overlayWindow.PositionChanged -= OnOverlayPositionChanged;
                        _overlayWindow.Close();
                        _overlayWindow = null;
                        Console.WriteLine("[OverlayManager] Overlay window disposed");
                    });
                }

                _isDisposed = true;
            }
        }

        #endregion
    }
}
