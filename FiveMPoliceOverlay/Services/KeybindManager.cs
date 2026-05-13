using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using FiveMPoliceOverlay.Models;

namespace FiveMPoliceOverlay.Services
{
    /// <summary>
    /// Manages global keyboard hooks and keybind-to-template mappings.
    /// Uses low-level keyboard hook (WH_KEYBOARD_LL) to capture keybinds even when FiveM has focus.
    /// </summary>
    public class KeybindManager : IDisposable
    {
        #region Windows API P/Invoke Declarations

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        // Virtual key codes for modifier keys
        private const int VK_SHIFT = 0x10;
        private const int VK_CONTROL = 0x11;
        private const int VK_MENU = 0x12; // Alt key
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        #endregion

        #region Fields

        private readonly Dictionary<string, KeybindRegistration> _keybindMap;
        private IntPtr _hookId = IntPtr.Zero;
        private LowLevelKeyboardProc? _hookCallback;
        private bool _isDisposed;

        #endregion

        #region Events

        /// <summary>
        /// Fired when a registered keybind is pressed.
        /// Provides the associated MessageTemplate.
        /// </summary>
        public event EventHandler<KeybindPressedEventArgs>? KeybindPressed;

        #endregion

        #region Constructor

        public KeybindManager()
        {
            _keybindMap = new Dictionary<string, KeybindRegistration>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the global keyboard hook to capture keybind events.
        /// </summary>
        public void StartHook()
        {
            if (_hookId != IntPtr.Zero)
            {
                return; // Already hooked
            }

            _hookCallback = HookCallback;
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                if (curModule != null && curModule.ModuleName != null)
                {
                    _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _hookCallback, 
                        GetModuleHandle(curModule.ModuleName), 0);
                }
            }

            if (_hookId == IntPtr.Zero)
            {
                Console.WriteLine("[KeybindManager] ERROR: Failed to install keyboard hook");
            }
            else
            {
                Console.WriteLine("[KeybindManager] Keyboard hook installed successfully");
            }
        }

        /// <summary>
        /// Stops the global keyboard hook.
        /// </summary>
        public void StopHook()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
                Console.WriteLine("[KeybindManager] Keyboard hook removed");
            }
        }

        /// <summary>
        /// Registers a keybind with an associated message template.
        /// </summary>
        /// <param name="keybind">The keybind definition</param>
        /// <param name="template">The message template to associate</param>
        /// <returns>True if registration successful, false if keybind already registered</returns>
        public bool RegisterKeybind(KeybindDefinition keybind, MessageTemplate template)
        {
            if (keybind == null)
                throw new ArgumentNullException(nameof(keybind));
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            string keybindKey = GetKeybindKey(keybind);

            if (_keybindMap.ContainsKey(keybindKey))
            {
                Console.WriteLine($"[KeybindManager] Keybind already registered: {keybind}");
                return false;
            }

            _keybindMap[keybindKey] = new KeybindRegistration(keybind, template);
            Console.WriteLine($"[KeybindManager] Registered keybind: {keybind} -> {template.Name}");
            return true;
        }

        /// <summary>
        /// Unregisters a keybind.
        /// </summary>
        /// <param name="keybind">The keybind to unregister</param>
        /// <returns>True if unregistration successful, false if keybind not found</returns>
        public bool UnregisterKeybind(KeybindDefinition keybind)
        {
            if (keybind == null)
                throw new ArgumentNullException(nameof(keybind));

            string keybindKey = GetKeybindKey(keybind);

            if (_keybindMap.Remove(keybindKey))
            {
                Console.WriteLine($"[KeybindManager] Unregistered keybind: {keybind}");
                return true;
            }

            Console.WriteLine($"[KeybindManager] Keybind not found: {keybind}");
            return false;
        }

        /// <summary>
        /// Checks if a keybind is available (not already registered).
        /// </summary>
        /// <param name="keybind">The keybind to check</param>
        /// <returns>True if available, false if already registered</returns>
        public bool IsKeybindAvailable(KeybindDefinition keybind)
        {
            if (keybind == null)
                throw new ArgumentNullException(nameof(keybind));

            string keybindKey = GetKeybindKey(keybind);
            return !_keybindMap.ContainsKey(keybindKey);
        }

        /// <summary>
        /// Gets all registered keybinds.
        /// </summary>
        /// <returns>Dictionary of keybind keys to registrations</returns>
        public IReadOnlyDictionary<string, KeybindRegistration> GetRegisteredKeybinds()
        {
            return _keybindMap;
        }

        /// <summary>
        /// Clears all registered keybinds.
        /// </summary>
        public void ClearAllKeybinds()
        {
            _keybindMap.Clear();
            Console.WriteLine("[KeybindManager] Cleared all keybinds");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Low-level keyboard hook callback.
        /// Captures keyboard events and checks for registered keybinds.
        /// </summary>
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                try
                {
                    var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    Key key = KeyInterop.KeyFromVirtualKey((int)hookStruct.vkCode);

                    // Get current modifier state
                    ModifierKeys modifiers = GetCurrentModifiers();

                    // Create keybind definition from current state
                    var currentKeybind = new KeybindDefinition
                    {
                        Modifiers = modifiers,
                        Key = key
                    };

                    // Check if this keybind is registered
                    string keybindKey = GetKeybindKey(currentKeybind);
                    if (_keybindMap.TryGetValue(keybindKey, out var registration))
                    {
                        OnKeybindPressed(registration.Template);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[KeybindManager] Error in hook callback: {ex.Message}");
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        /// <summary>
        /// Gets the current state of modifier keys.
        /// </summary>
        private ModifierKeys GetCurrentModifiers()
        {
            ModifierKeys modifiers = ModifierKeys.None;

            if (IsKeyPressed(VK_CONTROL))
                modifiers |= ModifierKeys.Control;

            if (IsKeyPressed(VK_SHIFT))
                modifiers |= ModifierKeys.Shift;

            if (IsKeyPressed(VK_MENU))
                modifiers |= ModifierKeys.Alt;

            if (IsKeyPressed(VK_LWIN) || IsKeyPressed(VK_RWIN))
                modifiers |= ModifierKeys.Windows;

            return modifiers;
        }

        /// <summary>
        /// Checks if a key is currently pressed.
        /// </summary>
        private bool IsKeyPressed(int vkCode)
        {
            return (GetKeyState(vkCode) & 0x8000) != 0;
        }

        /// <summary>
        /// Generates a unique key string for a keybind definition.
        /// Format: "Modifiers+Key" (e.g., "Control+Shift+F1")
        /// </summary>
        private string GetKeybindKey(KeybindDefinition keybind)
        {
            return $"{keybind.Modifiers}+{keybind.Key}";
        }

        /// <summary>
        /// Raises the KeybindPressed event.
        /// </summary>
        protected virtual void OnKeybindPressed(MessageTemplate template)
        {
            Console.WriteLine($"[KeybindManager] Keybind pressed: {template.Name}");
            KeybindPressed?.Invoke(this, new KeybindPressedEventArgs(template));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_isDisposed)
            {
                StopHook();
                _keybindMap.Clear();
                _isDisposed = true;
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents a registered keybind with its associated template.
    /// </summary>
    public class KeybindRegistration
    {
        public KeybindDefinition Keybind { get; }
        public MessageTemplate Template { get; }

        public KeybindRegistration(KeybindDefinition keybind, MessageTemplate template)
        {
            Keybind = keybind ?? throw new ArgumentNullException(nameof(keybind));
            Template = template ?? throw new ArgumentNullException(nameof(template));
        }
    }

    /// <summary>
    /// Event arguments for KeybindPressed event.
    /// </summary>
    public class KeybindPressedEventArgs : EventArgs
    {
        public MessageTemplate Template { get; }

        public KeybindPressedEventArgs(MessageTemplate template)
        {
            Template = template ?? throw new ArgumentNullException(nameof(template));
        }
    }
}
