using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using FiveMPoliceOverlay.Models;
using FiveMPoliceOverlay.Services;

namespace FiveMPoliceOverlay.Views
{
    /// <summary>
    /// Settings window for configuring keybinds, templates, and general application settings.
    /// All labels in Bahasa Indonesia per requirements.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly ConfigurationManager _configManager;
        private readonly OverlayManager? _overlayManager;
        private AppConfiguration _config;
        private bool _isDirty;
        private readonly DispatcherTimer _debounceTimer;

        private ObservableCollection<KeybindDisplayItem> _keybindItems = new();
        private ObservableCollection<TemplateDisplayItem> _predefinedTemplates = new();
        private ObservableCollection<TemplateDisplayItem> _customTemplates = new();

        public SettingsWindow(ConfigurationManager configManager, OverlayManager? overlayManager = null)
        {
            InitializeComponent();
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _overlayManager = overlayManager;
            _config = _configManager.LoadConfiguration();

            _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _debounceTimer.Tick += (s, e) => { _debounceTimer.Stop(); SaveConfiguration(); };

            LoadAllSettings();
        }

        #region Loading

        private void LoadAllSettings()
        {
            _config = _configManager.LoadConfiguration();
            LoadKeybinds();
            LoadTemplates();
            LoadGeneralSettings();
            _isDirty = false;
        }

        private void LoadKeybinds()
        {
            _keybindItems.Clear();
            foreach (var mapping in _config.Keybinds)
            {
                var template = _config.Templates.FirstOrDefault(t => t.Id == mapping.TemplateId);
                _keybindItems.Add(new KeybindDisplayItem
                {
                    Id = mapping.Id,
                    KeybindDisplay = mapping.Keybind.ToString(),
                    Category = template?.Category ?? "N/A",
                    PreviewText = template != null ? (template.Text.Length > 50 ? template.Text[..47] + "..." : template.Text) : "N/A",
                    TemplateId = mapping.TemplateId,
                    Keybind = mapping.Keybind
                });
            }
            KeybindDataGrid.ItemsSource = _keybindItems;
        }

        private void LoadTemplates()
        {
            _predefinedTemplates.Clear();
            _customTemplates.Clear();

            foreach (var t in _config.Templates)
            {
                var item = new TemplateDisplayItem
                {
                    Id = t.Id, Name = t.Name, Category = t.Category,
                    Text = t.Text, IsPredefined = t.IsPredefined
                };

                if (t.IsPredefined)
                    _predefinedTemplates.Add(item);
                else
                    _customTemplates.Add(item);
            }

            PredefinedTemplateList.ItemsSource = _predefinedTemplates;
            CustomTemplateDataGrid.ItemsSource = _customTemplates;
        }

        private void LoadGeneralSettings()
        {
            ChkAutoStart.IsChecked = AutoStartManager.IsAutoStartEnabled();
            ChkTestMode.IsChecked = _config.General.TestMode;
            UpdateModeStatus(_config.General.TestMode);
            TxtOverlayX.Text = _config.Overlay.Position.X.ToString("F0");
            TxtOverlayY.Text = _config.Overlay.Position.Y.ToString("F0");
        }

        #endregion

        #region Keybind Tab Events

        private void BtnAddKeybind_Click(object sender, RoutedEventArgs e)
        {
            ShowKeybindDialog(null);
        }

        private void BtnEditKeybind_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is KeybindDisplayItem item)
                ShowKeybindDialog(item);
        }

        private void BtnDeleteKeybind_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is KeybindDisplayItem item)
            {
                var result = MessageBox.Show($"Hapus keybind '{item.KeybindDisplay}'?", "Konfirmasi Hapus",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _config.Keybinds.RemoveAll(k => k.Id == item.Id);
                    MarkDirty();
                    LoadKeybinds();
                }
            }
        }

        private void ShowKeybindDialog(KeybindDisplayItem? existing)
        {
            var dialog = new Window
            {
                Title = existing == null ? "Tambah Keybind" : "Edit Keybind",
                Width = 450, Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this, ResizeMode = ResizeMode.NoResize,
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x1E, 0x1E, 0x2E))
            };

            var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(20) };

            // Keybind capture
            var lblKey = new System.Windows.Controls.TextBlock
            {
                Text = "Tekan kombinasi tombol:", Foreground = System.Windows.Media.Brushes.LightGray,
                FontSize = 12, Margin = new Thickness(0, 0, 0, 4)
            };
            var txtKey = new System.Windows.Controls.TextBox
            {
                IsReadOnly = true, FontSize = 16, FontWeight = FontWeights.Bold,
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x2A, 0x2A, 0x3D)),
                Foreground = System.Windows.Media.Brushes.White,
                Padding = new Thickness(10, 8, 10, 8),
                Text = existing?.KeybindDisplay ?? "Klik di sini lalu tekan tombol...",
                BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x44, 0x44, 0x66))
            };

            KeybindDefinition? capturedKeybind = existing?.Keybind;
            txtKey.PreviewKeyDown += (s, ev) =>
            {
                ev.Handled = true;
                var mods = Keyboard.Modifiers;
                var key = ev.Key == Key.System ? ev.SystemKey : ev.Key;
                if (key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftShift ||
                    key == Key.RightShift || key == Key.LeftAlt || key == Key.RightAlt ||
                    key == Key.LWin || key == Key.RWin)
                    return;

                capturedKeybind = new KeybindDefinition { Modifiers = mods, Key = key };
                txtKey.Text = capturedKeybind.ToString();
            };

            // Template selector
            var lblTpl = new System.Windows.Controls.TextBlock
            {
                Text = "Pilih template:", Foreground = System.Windows.Media.Brushes.LightGray,
                FontSize = 12, Margin = new Thickness(0, 12, 0, 4)
            };
            var cmbTemplate = new System.Windows.Controls.ComboBox
            {
                FontSize = 13, Padding = new Thickness(8, 6, 8, 6),
                DisplayMemberPath = "Name"
            };
            foreach (var t in _config.Templates)
                cmbTemplate.Items.Add(t);

            if (existing != null)
            {
                var sel = _config.Templates.FirstOrDefault(t => t.Id == existing.TemplateId);
                if (sel != null) cmbTemplate.SelectedItem = sel;
            }

            // Buttons
            var btnPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0)
            };
            var btnSave = new System.Windows.Controls.Button
            {
                Content = "Simpan", Width = 90, Padding = new Thickness(0, 8, 0, 8),
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x3D, 0x85, 0xC6)),
                Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 8, 0)
            };
            var btnCancelDlg = new System.Windows.Controls.Button
            {
                Content = "Batal", Width = 90, Padding = new Thickness(0, 8, 0, 8),
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x35, 0x35, 0x4D)),
                Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0)
            };

            btnSave.Click += (s, ev) =>
            {
                if (capturedKeybind == null || capturedKeybind.Key == Key.None)
                {
                    MessageBox.Show("Keybind tidak boleh kosong.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (cmbTemplate.SelectedItem is not MessageTemplate selectedTemplate)
                {
                    MessageBox.Show("Pilih template terlebih dahulu.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check duplicate
                string newKeyStr = capturedKeybind.ToString();
                bool isDuplicate = _config.Keybinds.Any(k =>
                    k.Keybind.ToString() == newKeyStr && (existing == null || k.Id != existing.Id));
                if (isDuplicate)
                {
                    MessageBox.Show($"Keybind '{newKeyStr}' sudah digunakan.", "Duplikat", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (existing != null)
                {
                    var mapping = _config.Keybinds.FirstOrDefault(k => k.Id == existing.Id);
                    if (mapping != null)
                    {
                        mapping.Keybind = capturedKeybind;
                        mapping.TemplateId = selectedTemplate.Id;
                    }
                }
                else
                {
                    _config.Keybinds.Add(new KeybindMapping
                    {
                        Id = Guid.NewGuid().ToString(),
                        Keybind = capturedKeybind,
                        TemplateId = selectedTemplate.Id
                    });
                }

                MarkDirty();
                LoadKeybinds();
                dialog.Close();
            };

            btnCancelDlg.Click += (s, ev) => dialog.Close();

            panel.Children.Add(lblKey);
            panel.Children.Add(txtKey);
            panel.Children.Add(lblTpl);
            panel.Children.Add(cmbTemplate);
            btnPanel.Children.Add(btnSave);
            btnPanel.Children.Add(btnCancelDlg);
            panel.Children.Add(btnPanel);
            dialog.Content = panel;
            dialog.ShowDialog();
        }

        #endregion

        #region Template Tab Events

        private void BtnEditPredefinedTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is TemplateDisplayItem item)
                ShowTemplateDialog(item, isPredefined: true);
        }

        private void BtnAddTemplate_Click(object sender, RoutedEventArgs e)
        {
            ShowTemplateDialog(null, isPredefined: false);
        }

        private void BtnEditCustomTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is TemplateDisplayItem item)
                ShowTemplateDialog(item, isPredefined: false);
        }

        private void BtnDeleteCustomTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is TemplateDisplayItem item)
            {
                var result = MessageBox.Show($"Hapus template '{item.Name}'?", "Konfirmasi Hapus",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _config.Templates.RemoveAll(t => t.Id == item.Id);
                    _config.Keybinds.RemoveAll(k => k.TemplateId == item.Id);
                    MarkDirty();
                    LoadTemplates();
                    LoadKeybinds();
                }
            }
        }

        private void ShowTemplateDialog(TemplateDisplayItem? existing, bool isPredefined)
        {
            var dialog = new Window
            {
                Title = existing == null ? "Tambah Template" : "Edit Template",
                Width = 500, Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this, ResizeMode = ResizeMode.NoResize,
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x1E, 0x1E, 0x2E))
            };

            var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(20) };

            // Name (disabled for predefined)
            var lblName = new System.Windows.Controls.TextBlock
            {
                Text = "Nama Template:", Foreground = System.Windows.Media.Brushes.LightGray,
                FontSize = 12, Margin = new Thickness(0, 0, 0, 4)
            };
            var txtName = new System.Windows.Controls.TextBox
            {
                Text = existing?.Name ?? "", FontSize = 13, IsEnabled = !isPredefined,
                Padding = new Thickness(8, 6, 8, 6),
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x2A, 0x2A, 0x3D)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x44, 0x44, 0x66))
            };

            // Category (disabled for predefined)
            var lblCat = new System.Windows.Controls.TextBlock
            {
                Text = "Kategori:", Foreground = System.Windows.Media.Brushes.LightGray,
                FontSize = 12, Margin = new Thickness(0, 10, 0, 4)
            };
            var txtCat = new System.Windows.Controls.TextBox
            {
                Text = existing?.Category ?? "BERITA LANGIT KOTA SIAGA",
                FontSize = 13, IsEnabled = !isPredefined,
                Padding = new Thickness(8, 6, 8, 6),
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x2A, 0x2A, 0x3D)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x44, 0x44, 0x66))
            };

            // Message text
            var lblMsg = new System.Windows.Controls.TextBlock
            {
                Text = "Isi Pesan:", Foreground = System.Windows.Media.Brushes.LightGray,
                FontSize = 12, Margin = new Thickness(0, 10, 0, 4)
            };
            var txtMsg = new System.Windows.Controls.TextBox
            {
                Text = existing?.Text ?? "", FontSize = 13, MaxLength = 200,
                AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
                Height = 100, VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                Padding = new Thickness(8, 6, 8, 6),
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x2A, 0x2A, 0x3D)),
                Foreground = System.Windows.Media.Brushes.White,
                CaretBrush = System.Windows.Media.Brushes.White,
                BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x44, 0x44, 0x66))
            };

            var lblCounter = new System.Windows.Controls.TextBlock
            {
                Text = $"{existing?.Text.Length ?? 0}/200 karakter",
                Foreground = System.Windows.Media.Brushes.Gray, FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 4, 0, 0)
            };
            txtMsg.TextChanged += (s, ev) =>
            {
                lblCounter.Text = $"{txtMsg.Text.Length}/200 karakter";
                lblCounter.Foreground = txtMsg.Text.Length > 180
                    ? System.Windows.Media.Brushes.OrangeRed
                    : System.Windows.Media.Brushes.Gray;
            };

            // Buttons
            var btnPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0)
            };
            var btnSave = new System.Windows.Controls.Button
            {
                Content = "Simpan", Width = 90, Padding = new Thickness(0, 8, 0, 8),
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x3D, 0x85, 0xC6)),
                Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 8, 0)
            };
            var btnCancelDlg = new System.Windows.Controls.Button
            {
                Content = "Batal", Width = 90, Padding = new Thickness(0, 8, 0, 8),
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x35, 0x35, 0x4D)),
                Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0)
            };

            btnSave.Click += (s, ev) =>
            {
                if (!isPredefined && string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Nama template tidak boleh kosong.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtMsg.Text))
                {
                    MessageBox.Show("Isi pesan tidak boleh kosong.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (existing != null)
                {
                    var template = _config.Templates.FirstOrDefault(t => t.Id == existing.Id);
                    if (template != null)
                    {
                        if (!isPredefined) { template.Name = txtName.Text.Trim(); template.Category = txtCat.Text.Trim(); }
                        template.Text = txtMsg.Text.Trim();
                    }
                }
                else
                {
                    _config.Templates.Add(new MessageTemplate
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = txtName.Text.Trim(),
                        Category = txtCat.Text.Trim(),
                        Text = txtMsg.Text.Trim(),
                        IsPredefined = false
                    });
                }

                MarkDirty();
                LoadTemplates();
                dialog.Close();
            };
            btnCancelDlg.Click += (s, ev) => dialog.Close();

            panel.Children.Add(lblName);
            panel.Children.Add(txtName);
            panel.Children.Add(lblCat);
            panel.Children.Add(txtCat);
            panel.Children.Add(lblMsg);
            panel.Children.Add(txtMsg);
            panel.Children.Add(lblCounter);
            btnPanel.Children.Add(btnSave);
            btnPanel.Children.Add(btnCancelDlg);
            panel.Children.Add(btnPanel);
            dialog.Content = panel;
            dialog.ShowDialog();
        }

        #endregion

        #region General Settings Events

        private void ChkAutoStart_Changed(object sender, RoutedEventArgs e)
        {
            if (ChkAutoStart.IsChecked == true)
                AutoStartManager.EnableAutoStart();
            else
                AutoStartManager.DisableAutoStart();
        }

        private void ChkTestMode_Changed(object sender, RoutedEventArgs e)
        {
            bool isTest = ChkTestMode.IsChecked == true;
            _config.General.TestMode = isTest;
            UpdateModeStatus(isTest);
            MarkDirty();
        }

        private void UpdateModeStatus(bool isTest)
        {
            if (isTest)
            {
                ModeStatusText.Text = "Mode saat ini: TEST (/me)";
                ModeStatusBorder.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xFF, 0xA7, 0x26));
            }
            else
            {
                ModeStatusText.Text = "Mode saat ini: PRODUCTION (/info)";
                ModeStatusBorder.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x4C, 0xAF, 0x50));
            }
        }

        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void BtnTestPosition_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(TxtOverlayX.Text, out double x) && double.TryParse(TxtOverlayY.Text, out double y))
            {
                _config.Overlay.Position = new Point(x, y);
                _overlayManager?.UpdatePosition(new Point(x, y));
                MarkDirty();
            }
            else
            {
                MessageBox.Show("Masukkan angka yang valid untuk posisi X dan Y.", "Validasi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Yakin ingin reset semua pengaturan ke default?\nTindakan ini tidak bisa dibatalkan.",
                "Konfirmasi Reset", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                _config = _configManager.ResetToDefaults();
                LoadAllSettings();
                MessageBox.Show("Pengaturan berhasil direset ke default.", "Reset Berhasil",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        #region Footer Buttons

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            ApplyOverlayPosition();
            SaveConfiguration();
            Close();
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            ApplyOverlayPosition();
            SaveConfiguration();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_isDirty)
            {
                var result = MessageBox.Show("Ada perubahan yang belum disimpan. Tutup tanpa menyimpan?",
                    "Konfirmasi", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No) return;
            }
            Close();
        }

        #endregion

        #region Helpers

        private void MarkDirty()
        {
            _isDirty = true;
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private void ApplyOverlayPosition()
        {
            if (double.TryParse(TxtOverlayX.Text, out double x) && double.TryParse(TxtOverlayY.Text, out double y))
            {
                _config.Overlay.Position = new Point(x, y);
            }
        }

        private void SaveConfiguration()
        {
            try
            {
                _configManager.SaveConfiguration(_config).Wait();
                _isDirty = false;
                Console.WriteLine("[SettingsWindow] Configuration saved");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SettingsWindow] ERROR saving config: {ex.Message}");
                MessageBox.Show($"Gagal menyimpan pengaturan: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
