using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FiveMPoliceOverlay.Views
{
    /// <summary>
    /// Transparent overlay window for FiveM Police Broadcast application.
    /// Displays status indicators and feedback while allowing click-through to FiveM.
    /// </summary>
    public partial class OverlayWindow : Window
    {
        #region Win32 API Imports

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;

        #endregion

        #region Fields

        private bool _isDragging = false;
        private Point _dragStartPoint;

        #endregion

        #region Events

        /// <summary>
        /// Event raised when the overlay position changes (for saving to configuration)
        /// </summary>
        public event EventHandler<Point>? PositionChanged;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new OverlayWindow with default position (10, 100)
        /// </summary>
        public OverlayWindow() : this(new Point(10, 100))
        {
        }

        /// <summary>
        /// Creates a new OverlayWindow with specified position from configuration
        /// </summary>
        /// <param name="position">Initial position (X, Y coordinates)</param>
        public OverlayWindow(Point position)
        {
            InitializeComponent();
            
            // Set initial position from configuration
            Left = position.X;
            Top = position.Y;
            
            // Enable drag-to-reposition functionality
            EnableDragToReposition();
        }

        #endregion

        /// <summary>
        /// Called when the window source is initialized.
        /// Sets up click-through behavior using WS_EX_TRANSPARENT flag.
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // Enable click-through by setting WS_EX_TRANSPARENT flag
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowExTransparent(hwnd);
        }

        #region Drag-to-Reposition

        /// <summary>
        /// Enables drag-to-reposition functionality for the overlay window.
        /// Temporarily disables click-through while dragging.
        /// </summary>
        private void EnableDragToReposition()
        {
            // Handle mouse down on the status indicator to start dragging
            StatusIndicator.MouseLeftButtonDown += OnMouseLeftButtonDown;
            StatusIndicator.MouseLeftButtonUp += OnMouseLeftButtonUp;
            StatusIndicator.MouseMove += OnMouseMove;
        }

        /// <summary>
        /// Handles mouse left button down event to start dragging
        /// </summary>
        private void OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isDragging = true;
            _dragStartPoint = e.GetPosition(this);
            StatusIndicator.CaptureMouse();
            
            // Temporarily disable click-through to allow dragging
            var hwnd = new WindowInteropHelper(this).Handle;
            RemoveWindowExTransparent(hwnd);
        }

        /// <summary>
        /// Handles mouse left button up event to stop dragging
        /// </summary>
        private void OnMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                StatusIndicator.ReleaseMouseCapture();
                
                // Re-enable click-through after dragging
                var hwnd = new WindowInteropHelper(this).Handle;
                SetWindowExTransparent(hwnd);
                
                // Raise PositionChanged event to save new position
                PositionChanged?.Invoke(this, new Point(Left, Top));
            }
        }

        /// <summary>
        /// Handles mouse move event to update window position while dragging
        /// </summary>
        private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point currentPosition = e.GetPosition(this);
                double offsetX = currentPosition.X - _dragStartPoint.X;
                double offsetY = currentPosition.Y - _dragStartPoint.Y;
                
                Left += offsetX;
                Top += offsetY;
            }
        }

        /// <summary>
        /// Removes the WS_EX_TRANSPARENT flag to allow mouse interaction
        /// </summary>
        private void RemoveWindowExTransparent(IntPtr hwnd)
        {
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
        }

        #endregion

        /// <summary>
        /// Sets the window to be transparent to mouse input (click-through).
        /// </summary>
        /// <param name="hwnd">Window handle</param>
        private void SetWindowExTransparent(IntPtr hwnd)
        {
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
        }

        /// <summary>
        /// Updates the mode indicator badge (Test/Production).
        /// </summary>
        /// <param name="isTestMode">True for test mode, false for production mode</param>
        public void SetMode(bool isTestMode)
        {
            Dispatcher.Invoke(() =>
            {
                if (isTestMode)
                {
                    ModeText.Text = "TEST";
                    ModeIndicator.Background = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(255, 152, 0)); // Orange
                }
                else
                {
                    ModeText.Text = "PROD";
                    ModeIndicator.Background = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(76, 175, 80)); // Green
                }
            });
        }

        /// <summary>
        /// Shows success feedback indicator for 2 seconds.
        /// </summary>
        public void ShowSuccessFeedback()
        {
            Dispatcher.Invoke(() =>
            {
                SuccessIndicator.Visibility = Visibility.Visible;
                
                // Hide after 2 seconds
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2)
                };
                timer.Tick += (s, e) =>
                {
                    SuccessIndicator.Visibility = Visibility.Collapsed;
                    timer.Stop();
                };
                timer.Start();
            });
        }

        /// <summary>
        /// Shows cooldown indicator with countdown timer.
        /// </summary>
        /// <param name="remainingSeconds">Remaining cooldown time in seconds</param>
        public void ShowCooldown(int remainingSeconds)
        {
            Dispatcher.Invoke(() =>
            {
                CooldownIndicator.Visibility = Visibility.Visible;
                CooldownText.Text = $"{remainingSeconds}s";
                
                // Update countdown every second
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                
                int remaining = remainingSeconds;
                timer.Tick += (s, e) =>
                {
                    remaining--;
                    if (remaining <= 0)
                    {
                        CooldownIndicator.Visibility = Visibility.Collapsed;
                        timer.Stop();
                    }
                    else
                    {
                        CooldownText.Text = $"{remaining}s";
                    }
                };
                timer.Start();
            });
        }

        /// <summary>
        /// Hides the cooldown indicator.
        /// </summary>
        public void HideCooldown()
        {
            Dispatcher.Invoke(() =>
            {
                CooldownIndicator.Visibility = Visibility.Collapsed;
            });
        }

        /// <summary>
        /// Updates the overlay position.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        public void UpdatePosition(double x, double y)
        {
            Dispatcher.Invoke(() =>
            {
                Left = x;
                Top = y;
                
                // Raise PositionChanged event to save new position
                PositionChanged?.Invoke(this, new Point(x, y));
            });
        }
    }
}
