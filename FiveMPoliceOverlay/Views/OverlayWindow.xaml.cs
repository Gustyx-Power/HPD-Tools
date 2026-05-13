using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using FiveMPoliceOverlay.Models;
using FiveMPoliceOverlay.Services;

namespace FiveMPoliceOverlay.Views
{
    #region Display Models

    public class KeybindDisplayItem
    {
        public string Id { get; set; } = string.Empty;
        public string KeybindDisplay { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string PreviewText { get; set; } = string.Empty;
        public string TemplateId { get; set; } = string.Empty;
        public KeybindDefinition? Keybind { get; set; }
    }

    public class TemplateDisplayItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public bool IsPredefined { get; set; }
        public string PreviewText => Text.Length > 50 ? Text[..47] + "..." : Text;
    }

    #endregion

    /// <summary>
    /// ReShade-style overlay window with collapsed badge and expandable interactive panel.
    /// Toggle with Ctrl+F10: collapsed = click-through badge, expanded = full interactive panel.
    /// </summary>
    public partial class OverlayWindow : Window
    {
        #region Win32

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;

        #endregion

        #region Fields

        private bool _isExpanded;
        private bool _isLoading;
        private ConfigurationManager? _configManager;
        private KeybindManager? _keybindManager;
        private AppConfiguration? _config;
        private ObservableCollection<KeybindDisplayItem> _keybindItems = new();
        private ObservableCollection<TemplateDisplayItem> _predefinedTemplates = new();
        private ObservableCollection<TemplateDisplayItem> _customTemplates = new();

        // Collapsed size
        private const double CollapsedW = 50, CollapsedH = 80;
        // Expanded size
        private const double ExpandedW = 620, ExpandedH = 500;

        #endregion

        #region Events

        public event EventHandler<Point>? PositionChanged;

        #endregion

        #region Constructor

        public OverlayWindow() : this(new Point(10, 100)) { }

        public OverlayWindow(Point position)
        {
            InitializeComponent();
            Left = position.X;
            Top = position.Y;
            Width = CollapsedW;
            Height = CollapsedH;
            _isExpanded = false;
        }

        /// <summary>
        /// Injects ConfigurationManager for settings access. Called after construction.
        /// </summary>
        public void SetConfigManager(ConfigurationManager configManager)
        {
            _configManager = configManager;
            _config = _configManager.LoadConfiguration();
            LoadAllData();
        }

        /// <summary>
        /// Injects KeybindManager to synchronize global hooks dynamically. Called after construction.
        /// </summary>
        public void SetKeybindManager(KeybindManager keybindManager)
        {
            _keybindManager = keybindManager;
        }

        #endregion

        #region Source Init

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // Start in click-through mode
            SetClickThrough(true);
        }

        #endregion

        #region Expand / Collapse (ReShade Toggle)

        /// <summary>
        /// Returns whether the panel is currently expanded.
        /// </summary>
        public bool IsExpanded => _isExpanded;

        /// <summary>
        /// Toggles between collapsed badge and expanded interactive panel.
        /// </summary>
        public void TogglePanel()
        {
            if (_isExpanded)
                CollapsePanel();
            else
                ExpandPanel();
        }

        /// <summary>
        /// Expands to full interactive panel. Disables click-through.
        /// </summary>
        public void ExpandPanel()
        {
            Dispatcher.Invoke(() =>
            {
                _isExpanded = true;

                // Reload config data
                if (_configManager != null)
                {
                    _config = _configManager.LoadConfiguration();
                    LoadAllData();
                }

                // Resize
                Width = ExpandedW;
                Height = ExpandedH;

                // Show panel, hide badge
                CollapsedView.Visibility = Visibility.Collapsed;
                ExpandedView.Visibility = Visibility.Visible;

                // Disable click-through so user can interact
                SetClickThrough(false);

                // Reset to Home tab
                TabHome.IsChecked = true;
                ShowTab("home");

                Console.WriteLine("[OverlayWindow] Panel expanded");
            });
        }

        /// <summary>
        /// Collapses to small badge. Re-enables click-through.
        /// </summary>
        public void CollapsePanel()
        {
            Dispatcher.Invoke(() =>
            {
                _isExpanded = false;

                // Save config if changed
                SaveConfig();

                // Resize
                Width = CollapsedW;
                Height = CollapsedH;

                // Hide panel, show badge
                ExpandedView.Visibility = Visibility.Collapsed;
                CollapsedView.Visibility = Visibility.Visible;

                // Re-enable click-through
                SetClickThrough(true);

                Console.WriteLine("[OverlayWindow] Panel collapsed");
            });
        }

        #endregion

        #region Click-Through Control

        private void SetClickThrough(bool enable)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            var style = GetWindowLong(hwnd, GWL_EXSTYLE);
            if (enable)
                SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_TRANSPARENT | WS_EX_LAYERED);
            else
                SetWindowLong(hwnd, GWL_EXSTYLE, (style & ~WS_EX_TRANSPARENT) | WS_EX_LAYERED);
        }

        #endregion

        #region Title Bar Drag

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
                DragMove();
        }

        private void BtnCollapse_Click(object sender, RoutedEventArgs e) => CollapsePanel();

        #endregion

        #region Tab Navigation

        private void Tab_Changed(object sender, RoutedEventArgs e)
        {
            // Guard: event fires during InitializeComponent before elements exist
            if (!IsLoaded) return;

            if (TabHome.IsChecked == true) ShowTab("home");
            else if (TabKeybinds.IsChecked == true) ShowTab("keybinds");
            else if (TabTemplates.IsChecked == true) ShowTab("templates");
            else if (TabSettings.IsChecked == true) ShowTab("settings");
        }

        private void ShowTab(string tab)
        {
            if (HomeContent == null) return; // guard during init
            HomeContent.Visibility = tab == "home" ? Visibility.Visible : Visibility.Collapsed;
            KeybindsContent.Visibility = tab == "keybinds" ? Visibility.Visible : Visibility.Collapsed;
            TemplatesContent.Visibility = tab == "templates" ? Visibility.Visible : Visibility.Collapsed;
            SettingsContent.Visibility = tab == "settings" ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        #region Data Loading

        private void LoadAllData()
        {
            if (_config == null) return;
            _isLoading = true;
            try
            {
                LoadKeybinds();
                LoadTemplates();
                LoadSettings();
                UpdateHomeStatus();
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void LoadKeybinds()
        {
            _keybindItems.Clear();
            if (_config == null) return;
            foreach (var m in _config.Keybinds)
            {
                var t = _config.Templates.FirstOrDefault(x => x.Id == m.TemplateId);
                _keybindItems.Add(new KeybindDisplayItem
                {
                    Id = m.Id,
                    KeybindDisplay = m.Keybind.ToString(),
                    Category = t?.Category ?? "N/A",
                    PreviewText = t != null ? (t.Text.Length > 40 ? t.Text[..37] + "..." : t.Text) : "N/A",
                    TemplateId = m.TemplateId,
                    Keybind = m.Keybind
                });
            }
            KeybindDataGrid.ItemsSource = _keybindItems;
        }

        private void LoadTemplates()
        {
            _predefinedTemplates.Clear();
            _customTemplates.Clear();
            if (_config == null) return;
            foreach (var t in _config.Templates)
            {
                var item = new TemplateDisplayItem
                { Id = t.Id, Name = t.Name, Category = t.Category, Text = t.Text, IsPredefined = t.IsPredefined };
                if (t.IsPredefined) _predefinedTemplates.Add(item);
                else _customTemplates.Add(item);
            }
            PredefinedTemplateList.ItemsSource = _predefinedTemplates;
            CustomTemplateDataGrid.ItemsSource = _customTemplates;
        }

        private void LoadSettings()
        {
            if (_config == null) return;
            ChkTestMode.IsChecked = _config.General.TestMode;
            ChkAutoStart.IsChecked = AutoStartManager.IsAutoStartEnabled();
            UpdateModeDisplay(_config.General.TestMode);
        }

        private void UpdateHomeStatus()
        {
            if (_config == null) return;
            TxtModeStatus.Text = _config.General.TestMode ? "Mode: TEST (/me)" : "Mode: PRODUCTION (/info)";
            TxtKeybindCount.Text = $"Keybind aktif: {_config.Keybinds.Count}";
        }

        #endregion

        #region Public Methods (called by OverlayManager)

        public void SetMode(bool isTestMode)
        {
            Dispatcher.Invoke(() =>
            {
                if (isTestMode)
                {
                    ModeText.Text = "TEST";
                    ModeIndicator.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xA7, 0x26));
                    ExpandedModeText.Text = "TEST";
                    ExpandedModeBadge.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xA7, 0x26));
                }
                else
                {
                    ModeText.Text = "PROD";
                    ModeIndicator.Background = new SolidColorBrush(Color.FromRgb(0x43, 0xA0, 0x47));
                    ExpandedModeText.Text = "PROD";
                    ExpandedModeBadge.Background = new SolidColorBrush(Color.FromRgb(0x43, 0xA0, 0x47));
                }
            });
        }

        public void ShowSuccessFeedback()
        {
            Dispatcher.Invoke(() =>
            {
                SuccessIndicator.Visibility = Visibility.Visible;
                var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                timer.Tick += (s, e) => { SuccessIndicator.Visibility = Visibility.Collapsed; timer.Stop(); };
                timer.Start();
            });
        }

        public void ShowCooldown(int seconds)
        {
            Dispatcher.Invoke(() =>
            {
                CooldownIndicator.Visibility = Visibility.Visible;
                CooldownText.Text = $"{seconds}s";
                int rem = seconds;
                var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                timer.Tick += (s, e) =>
                {
                    rem--;
                    if (rem <= 0) { CooldownIndicator.Visibility = Visibility.Collapsed; timer.Stop(); }
                    else CooldownText.Text = $"{rem}s";
                };
                timer.Start();
            });
        }

        public void HideCooldown()
        {
            Dispatcher.Invoke(() => CooldownIndicator.Visibility = Visibility.Collapsed);
        }

        public void UpdatePosition(double x, double y)
        {
            Dispatcher.Invoke(() => { Left = x; Top = y; PositionChanged?.Invoke(this, new Point(x, y)); });
        }

        public void SetFiveMStatus(bool detected)
        {
            Dispatcher.Invoke(() =>
            {
                TxtFiveMStatus.Text = detected ? "FiveM: Terdeteksi ✓" : "FiveM: Tidak terdeteksi";
                TxtFiveMStatus.Foreground = detected
                    ? (Brush)FindResource("SuccessBrush")
                    : (Brush)FindResource("DangerBrush");
            });
        }

        #endregion

        #region Keybind Tab Events

        private void BtnAddKeybind_Click(object sender, RoutedEventArgs e) => ShowKeybindDialog(null);

        private void BtnEditKeybind_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is KeybindDisplayItem item)
                ShowKeybindDialog(item);
        }

        private void BtnDeleteKeybind_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is KeybindDisplayItem item)
            {
                if (MessageBox.Show($"Hapus keybind '{item.KeybindDisplay}'?", "Konfirmasi",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _config?.Keybinds.RemoveAll(k => k.Id == item.Id);
                    SaveConfig();
                    LoadKeybinds();
                    UpdateHomeStatus();
                }
            }
        }

        private void ShowKeybindDialog(KeybindDisplayItem? existing)
        {
            if (_config == null) return;
            var dlg = new Window
            {
                Title = existing == null ? "Tambah Keybind" : "Edit Keybind",
                Width = 420, Height = 280, WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Topmost = true, ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x2E))
            };
            var sp = new System.Windows.Controls.StackPanel { Margin = new Thickness(16) };

            var txtKey = new System.Windows.Controls.TextBox
            {
                IsReadOnly = true, FontSize = 15, FontWeight = FontWeights.Bold, Padding = new Thickness(8, 6, 8, 6),
                Text = existing?.KeybindDisplay ?? "Tekan tombol...",
                Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x3D)),
                Foreground = Brushes.White, CaretBrush = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x66))
            };
            KeybindDefinition? cap = existing?.Keybind;
            txtKey.PreviewKeyDown += (s, ev) =>
            {
                ev.Handled = true;
                var k = ev.Key == Key.System ? ev.SystemKey : ev.Key;
                if (k == Key.LeftCtrl || k == Key.RightCtrl || k == Key.LeftShift || k == Key.RightShift ||
                    k == Key.LeftAlt || k == Key.RightAlt || k == Key.LWin || k == Key.RWin) return;
                cap = new KeybindDefinition { Modifiers = Keyboard.Modifiers, Key = k };
                txtKey.Text = cap.ToString();
            };

            var cmbTpl = new System.Windows.Controls.ComboBox { FontSize = 12, Margin = new Thickness(0, 8, 0, 0), DisplayMemberPath = "Name" };
            foreach (var t in _config.Templates) cmbTpl.Items.Add(t);
            if (existing != null) cmbTpl.SelectedItem = _config.Templates.FirstOrDefault(t => t.Id == existing.TemplateId);

            var btnRow = new System.Windows.Controls.StackPanel
            { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
            var btnSave = new System.Windows.Controls.Button
            { Content = "Simpan", Width = 80, Padding = new Thickness(0, 6, 0, 6),
              Background = new SolidColorBrush(Color.FromRgb(0x3D, 0x85, 0xC6)), Foreground = Brushes.White, BorderThickness = new Thickness(0), Margin = new Thickness(0, 0, 6, 0) };
            var btnCancel = new System.Windows.Controls.Button
            { Content = "Batal", Width = 80, Padding = new Thickness(0, 6, 0, 6),
              Background = new SolidColorBrush(Color.FromRgb(0x35, 0x35, 0x4D)), Foreground = Brushes.White, BorderThickness = new Thickness(0) };

            btnSave.Click += (s, ev) =>
            {
                if (cap == null || cap.Key == Key.None) { MessageBox.Show("Keybind kosong."); return; }
                if (cmbTpl.SelectedItem is not MessageTemplate sel) { MessageBox.Show("Pilih template."); return; }
                string k = cap.ToString();
                if (_config.Keybinds.Any(x => x.Keybind.ToString() == k && (existing == null || x.Id != existing.Id)))
                { MessageBox.Show($"Keybind '{k}' sudah dipakai."); return; }

                if (existing != null)
                { var m = _config.Keybinds.FirstOrDefault(x => x.Id == existing.Id); if (m != null) { m.Keybind = cap; m.TemplateId = sel.Id; } }
                else
                { _config.Keybinds.Add(new KeybindMapping { Id = Guid.NewGuid().ToString(), Keybind = cap, TemplateId = sel.Id }); }
                SaveConfig(); LoadKeybinds(); UpdateHomeStatus(); dlg.Close();
            };
            btnCancel.Click += (s, ev) => dlg.Close();

            sp.Children.Add(new System.Windows.Controls.TextBlock { Text = "Tekan kombinasi tombol:", Foreground = Brushes.Gray, FontSize = 11, Margin = new Thickness(0, 0, 0, 4) });
            sp.Children.Add(txtKey);
            sp.Children.Add(new System.Windows.Controls.TextBlock { Text = "Pilih template:", Foreground = Brushes.Gray, FontSize = 11, Margin = new Thickness(0, 8, 0, 4) });
            sp.Children.Add(cmbTpl);
            btnRow.Children.Add(btnSave); btnRow.Children.Add(btnCancel);
            sp.Children.Add(btnRow);
            dlg.Content = sp;
            dlg.ShowDialog();
        }

        #endregion

        #region Template Tab Events

        private void BtnEditPredefinedTemplate_Click(object sender, RoutedEventArgs e)
        { if (sender is System.Windows.Controls.Button btn && btn.Tag is TemplateDisplayItem item) ShowTemplateDialog(item, true); }

        private void BtnAddTemplate_Click(object sender, RoutedEventArgs e) => ShowTemplateDialog(null, false);

        private void BtnEditCustomTemplate_Click(object sender, RoutedEventArgs e)
        { if (sender is System.Windows.Controls.Button btn && btn.Tag is TemplateDisplayItem item) ShowTemplateDialog(item, false); }

        private void BtnDeleteCustomTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is TemplateDisplayItem item)
            {
                if (MessageBox.Show($"Hapus template '{item.Name}'?", "Konfirmasi",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _config?.Templates.RemoveAll(t => t.Id == item.Id);
                    _config?.Keybinds.RemoveAll(k => k.TemplateId == item.Id);
                    SaveConfig(); LoadTemplates(); LoadKeybinds(); UpdateHomeStatus();
                }
            }
        }

        private void ShowTemplateDialog(TemplateDisplayItem? existing, bool isPredefined)
        {
            if (_config == null) return;
            var dlg = new Window
            {
                Title = existing == null ? "Tambah Template" : "Edit Template",
                Width = 460, Height = 380, WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Topmost = true, ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x2E))
            };
            var sp = new System.Windows.Controls.StackPanel { Margin = new Thickness(16) };

            var txtName = new System.Windows.Controls.TextBox
            { Text = existing?.Name ?? "", FontSize = 12, IsEnabled = !isPredefined, Padding = new Thickness(6, 4, 6, 4),
              Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x3D)), Foreground = Brushes.White,
              BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x66)) };

            var txtCat = new System.Windows.Controls.TextBox
            { Text = existing?.Category ?? "BERITA LANGIT KOTA SIAGA", FontSize = 12, IsEnabled = !isPredefined, Padding = new Thickness(6, 4, 6, 4),
              Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x3D)), Foreground = Brushes.White,
              BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x66)) };

            var txtMsg = new System.Windows.Controls.TextBox
            { Text = existing?.Text ?? "", FontSize = 12, MaxLength = 200, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
              Height = 90, Padding = new Thickness(6, 4, 6, 4),
              Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x3D)), Foreground = Brushes.White,
              CaretBrush = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x66)) };

            var lblCount = new System.Windows.Controls.TextBlock { Text = $"{txtMsg.Text.Length}/200", Foreground = Brushes.Gray, FontSize = 10, HorizontalAlignment = HorizontalAlignment.Right };
            txtMsg.TextChanged += (s, ev) => lblCount.Text = $"{txtMsg.Text.Length}/200";

            var btnRow = new System.Windows.Controls.StackPanel
            { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
            var btnSave = new System.Windows.Controls.Button
            { Content = "Simpan", Width = 80, Padding = new Thickness(0, 6, 0, 6),
              Background = new SolidColorBrush(Color.FromRgb(0x3D, 0x85, 0xC6)), Foreground = Brushes.White, BorderThickness = new Thickness(0), Margin = new Thickness(0, 0, 6, 0) };
            var btnCancel = new System.Windows.Controls.Button
            { Content = "Batal", Width = 80, Padding = new Thickness(0, 6, 0, 6),
              Background = new SolidColorBrush(Color.FromRgb(0x35, 0x35, 0x4D)), Foreground = Brushes.White, BorderThickness = new Thickness(0) };

            btnSave.Click += (s, ev) =>
            {
                if (!isPredefined && string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("Nama kosong."); return; }
                if (string.IsNullOrWhiteSpace(txtMsg.Text)) { MessageBox.Show("Pesan kosong."); return; }
                if (existing != null)
                {
                    var t = _config.Templates.FirstOrDefault(x => x.Id == existing.Id);
                    if (t != null) { if (!isPredefined) { t.Name = txtName.Text.Trim(); t.Category = txtCat.Text.Trim(); } t.Text = txtMsg.Text.Trim(); }
                }
                else
                {
                    _config.Templates.Add(new MessageTemplate { Id = Guid.NewGuid().ToString(), Name = txtName.Text.Trim(),
                        Category = txtCat.Text.Trim(), Text = txtMsg.Text.Trim(), IsPredefined = false });
                }
                SaveConfig(); LoadTemplates(); dlg.Close();
            };
            btnCancel.Click += (s, ev) => dlg.Close();

            sp.Children.Add(new System.Windows.Controls.TextBlock { Text = "Nama:", Foreground = Brushes.Gray, FontSize = 11, Margin = new Thickness(0, 0, 0, 3) });
            sp.Children.Add(txtName);
            sp.Children.Add(new System.Windows.Controls.TextBlock { Text = "Kategori:", Foreground = Brushes.Gray, FontSize = 11, Margin = new Thickness(0, 8, 0, 3) });
            sp.Children.Add(txtCat);
            sp.Children.Add(new System.Windows.Controls.TextBlock { Text = "Isi Pesan:", Foreground = Brushes.Gray, FontSize = 11, Margin = new Thickness(0, 8, 0, 3) });
            sp.Children.Add(txtMsg);
            sp.Children.Add(lblCount);
            btnRow.Children.Add(btnSave); btnRow.Children.Add(btnCancel);
            sp.Children.Add(btnRow);
            dlg.Content = sp;
            dlg.ShowDialog();
        }

        #endregion

        #region Settings Tab Events

        private void ChkTestMode_Changed(object sender, RoutedEventArgs e)
        {
            if (_config == null || _isLoading) return;
            bool isTest = ChkTestMode.IsChecked == true;
            _config.General.TestMode = isTest;
            UpdateModeDisplay(isTest);
            SetMode(isTest);
            SaveConfig();
            UpdateHomeStatus();
        }

        private void UpdateModeDisplay(bool isTest)
        {
            if (isTest)
            {
                ModeStatusText.Text = "TEST (/me)";
                ModeStatusBorder.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xA7, 0x26));
            }
            else
            {
                ModeStatusText.Text = "PRODUCTION (/info)";
                ModeStatusBorder.Background = new SolidColorBrush(Color.FromRgb(0x43, 0xA0, 0x47));
            }
        }

        private void ChkAutoStart_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;
            if (ChkAutoStart.IsChecked == true) AutoStartManager.EnableAutoStart();
            else AutoStartManager.DisableAutoStart();
        }

        private void BtnResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Reset semua pengaturan ke default?", "Konfirmasi",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes && _configManager != null)
            {
                _config = _configManager.ResetToDefaults();
                LoadAllData();
                SyncKeybindHooks();
            }
        }

        #endregion

        #region Config Save

        private void SaveConfig()
        {
            if (_config == null || _configManager == null) return;
            try 
            { 
                _configManager.SaveConfiguration(_config).Wait(); 
                SyncKeybindHooks();
            }
            catch (Exception ex) { Console.WriteLine($"[OverlayWindow] Save error: {ex.Message}"); }
        }

        private void SyncKeybindHooks()
        {
            if (_keybindManager == null || _config == null) return;

            try
            {
                _keybindManager.ClearAllKeybinds();

                // Re-register toggle keybind to maintain overlay collapse/expand functionality
                var toggleTemplate = new MessageTemplate
                {
                    Id = "toggle-overlay",
                    Category = "System",
                    Name = "Toggle Overlay",
                    Text = "",
                    IsPredefined = true
                };
                _keybindManager.RegisterKeybind(_config.Overlay.ToggleKeybind, toggleTemplate);

                // Re-register all mapping bindings
                foreach (var mapping in _config.Keybinds)
                {
                    var template = _config.Templates.FirstOrDefault(t => t.Id == mapping.TemplateId);
                    if (template != null)
                    {
                        _keybindManager.RegisterKeybind(mapping.Keybind, template);
                    }
                }
                Console.WriteLine("[OverlayWindow] Successfully synchronized KeybindManager hooks with current configuration.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OverlayWindow] Error synchronizing keybind hooks: {ex.Message}");
            }
        }

        #endregion
    }
}
