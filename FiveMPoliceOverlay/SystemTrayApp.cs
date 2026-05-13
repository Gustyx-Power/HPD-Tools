using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace FiveMPoliceOverlay
{
    /// <summary>
    /// Manages the system tray NotifyIcon for background operation.
    /// Provides context menu for settings, overlay toggle, and exit.
    /// </summary>
    public class SystemTrayApp : IDisposable
    {
        private NotifyIcon? _notifyIcon;
        private ContextMenuStrip? _contextMenu;
        private readonly Services.OverlayManager _overlayManager;
        private readonly Services.ConfigurationManager _configManager;
        private bool _isDisposed;

        /// <summary>
        /// Gets or sets whether FiveM is currently detected (for tooltip update).
        /// </summary>
        public bool IsFiveMDetected
        {
            set => UpdateTooltip(value);
        }

        public SystemTrayApp(Services.OverlayManager overlayManager, Services.ConfigurationManager configManager)
        {
            _overlayManager = overlayManager ?? throw new ArgumentNullException(nameof(overlayManager));
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            Initialize();
        }

        private void Initialize()
        {
            _contextMenu = new ContextMenuStrip();
            _contextMenu.BackColor = Color.FromArgb(30, 30, 46);
            _contextMenu.ForeColor = Color.White;
            _contextMenu.Renderer = new DarkMenuRenderer();

            var settingsItem = new ToolStripMenuItem("⚙  Pengaturan");
            settingsItem.Click += OnSettingsClicked;

            var toggleItem = new ToolStripMenuItem("👁  Toggle Overlay (F10)");
            toggleItem.Click += OnToggleOverlayClicked;

            var separator = new ToolStripSeparator();

            var exitItem = new ToolStripMenuItem("❌  Keluar");
            exitItem.Click += OnExitClicked;

            _contextMenu.Items.AddRange(new ToolStripItem[] { settingsItem, toggleItem, separator, exitItem });

            _notifyIcon = new NotifyIcon
            {
                Icon = LoadAppIcon(),
                Text = "HOPE PD SkyNews - Menunggu FiveM...",
                Visible = true,
                ContextMenuStrip = _contextMenu
            };

            _notifyIcon.DoubleClick += OnSettingsClicked;
        }

        private Icon LoadAppIcon()
        {
            try
            {
                // Try loading from app directory
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(iconPath))
                    return new Icon(iconPath);
            }
            catch { /* fallback below */ }

            // Fallback: use default application icon
            return SystemIcons.Application;
        }

        private void UpdateTooltip(bool fiveMDetected)
        {
            if (_notifyIcon == null) return;

            _notifyIcon.Text = fiveMDetected
                ? "HOPE PD SkyNews - FiveM Terdeteksi ✓"
                : "HOPE PD SkyNews - Menunggu FiveM...";
        }

        private void OnSettingsClicked(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Toggle the overlay panel (ReShade-style)
                _overlayManager.ToggleOverlay();
            });
        }

        private void OnToggleOverlayClicked(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => _overlayManager.ToggleOverlay());
        }

        private void OnExitClicked(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
        }

        /// <summary>
        /// Shows a balloon notification in the system tray.
        /// </summary>
        public void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            _notifyIcon?.ShowBalloonTip(3000, title, message, icon);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                    _notifyIcon = null;
                }
                _contextMenu?.Dispose();
                _isDisposed = true;
            }
        }
    }

    /// <summary>
    /// Custom dark theme renderer for the tray context menu.
    /// </summary>
    internal class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkMenuColorTable()) { }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = Color.White;
            base.OnRenderItemText(e);
        }
    }

    internal class DarkMenuColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => Color.FromArgb(50, 50, 70);
        public override Color MenuItemBorder => Color.FromArgb(60, 60, 80);
        public override Color MenuBorder => Color.FromArgb(50, 50, 70);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(50, 50, 70);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(50, 50, 70);
        public override Color ToolStripDropDownBackground => Color.FromArgb(30, 30, 46);
        public override Color ImageMarginGradientBegin => Color.FromArgb(30, 30, 46);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(30, 30, 46);
        public override Color ImageMarginGradientEnd => Color.FromArgb(30, 30, 46);
        public override Color SeparatorDark => Color.FromArgb(60, 60, 80);
        public override Color SeparatorLight => Color.FromArgb(60, 60, 80);
    }
}
