using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace FiveMPoliceOverlay.Infrastructure
{
    /// <summary>
    /// Low-level keyboard input simulation using Windows SendInput API.
    /// This service simulates keyboard input at user-level (safe for anti-cheat systems).
    /// </summary>
    public class KeyboardSimulator
    {
        #region Windows API P/Invoke Declarations

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern ushort VkKeyScanEx(char ch, IntPtr dwhkl);

        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint idThread);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalFree(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern UIntPtr GlobalSize(IntPtr hMem);

        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_UNICODE = 0x0004;
        private const uint KEYEVENTF_SCANCODE = 0x0008;

        // Clipboard constants
        private const uint CF_UNICODETEXT = 13;
        private const uint GMEM_MOVEABLE = 0x0002;
        private const uint GMEM_ZEROINIT = 0x0040;
        private const uint GHND = GMEM_MOVEABLE | GMEM_ZEROINIT;

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        #endregion

        #region Virtual Key Codes

        /// <summary>
        /// Common virtual key codes for keyboard simulation.
        /// </summary>
        public enum VirtualKeyCode : ushort
        {
            RETURN = 0x0D,      // Enter key
            SHIFT = 0x10,       // Shift key
            CONTROL = 0x11,     // Ctrl key
            MENU = 0x12,        // Alt key
            ESCAPE = 0x1B,      // Esc key
            SPACE = 0x20,       // Space bar
            BACK = 0x08,        // Backspace
            TAB = 0x09,         // Tab key
            
            // Letter keys
            VK_T = 0x54,        // T key (for FiveM chat)
            VK_V = 0x56,        // V key (for paste)
            
            // Function keys
            F1 = 0x70,
            F2 = 0x71,
            F3 = 0x72,
            F4 = 0x73,
            F5 = 0x74,
            F6 = 0x75,
            F7 = 0x76,
            F8 = 0x77,
            F9 = 0x78,
            F10 = 0x79,
            F11 = 0x7A,
            F12 = 0x7B,
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Types a string of text by simulating keyboard input for each character.
        /// Uses Unicode input for international character support.
        /// </summary>
        /// <param name="text">The text to type</param>
        /// <returns>True if all characters were sent successfully, false otherwise</returns>
        public bool TypeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return true;

            foreach (char c in text)
            {
                if (!TypeCharacter(c))
                    return false;
                
                // Small delay between characters to simulate natural typing
                Thread.Sleep(10);
            }

            return true;
        }

        /// <summary>
        /// Types text with optimization for long messages.
        /// For messages >100 characters, uses clipboard paste (Ctrl+V) for better performance.
        /// Falls back to TypeText if clipboard paste fails.
        /// </summary>
        /// <param name="text">The text to type</param>
        /// <returns>True if text was sent successfully, false otherwise</returns>
        public bool TypeTextOptimized(string text)
        {
            if (string.IsNullOrEmpty(text))
                return true;

            // For short messages, use regular typing
            if (text.Length <= 100)
            {
                return TypeText(text);
            }

            // For long messages, try clipboard paste optimization
            string? originalClipboard = null;
            bool clipboardRestored = false;

            try
            {
                // Save original clipboard content
                originalClipboard = GetClipboardText();

                // Set new clipboard content
                if (!SetClipboardText(text))
                {
                    // Clipboard operation failed, fallback to typing
                    return TypeText(text);
                }

                // Simulate Ctrl+V (paste)
                if (!KeyDown(VirtualKeyCode.CONTROL))
                    return false;

                Thread.Sleep(10);

                if (!PressKey(VirtualKeyCode.VK_V))
                {
                    KeyUp(VirtualKeyCode.CONTROL);
                    return false;
                }

                Thread.Sleep(10);

                if (!KeyUp(VirtualKeyCode.CONTROL))
                    return false;

                // Wait for paste to complete
                Thread.Sleep(50);

                // Restore original clipboard
                if (originalClipboard != null)
                {
                    SetClipboardText(originalClipboard);
                    clipboardRestored = true;
                }
                else
                {
                    ClearClipboard();
                    clipboardRestored = true;
                }

                return true;
            }
            catch
            {
                // If anything fails, try to restore clipboard and fallback to typing
                if (!clipboardRestored && originalClipboard != null)
                {
                    try
                    {
                        SetClipboardText(originalClipboard);
                    }
                    catch
                    {
                        // Ignore clipboard restore errors
                    }
                }

                // Fallback to regular typing
                return TypeText(text);
            }
        }

        /// <summary>
        /// Presses and releases a key (complete key press).
        /// </summary>
        /// <param name="keyCode">The virtual key code to press</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool PressKey(VirtualKeyCode keyCode)
        {
            if (!KeyDown(keyCode))
                return false;

            Thread.Sleep(50); // Brief delay between down and up

            if (!KeyUp(keyCode))
                return false;

            return true;
        }

        /// <summary>
        /// Simulates pressing a key down (without releasing).
        /// </summary>
        /// <param name="keyCode">The virtual key code to press down</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool KeyDown(VirtualKeyCode keyCode)
        {
            var input = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = (ushort)keyCode,
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            uint result = SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
            return result != 0;
        }

        /// <summary>
        /// Simulates releasing a key (key up).
        /// </summary>
        /// <param name="keyCode">The virtual key code to release</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool KeyUp(VirtualKeyCode keyCode)
        {
            var input = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = (ushort)keyCode,
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            uint result = SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
            return result != 0;
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Types a single character using Unicode input.
        /// This method supports international characters and special symbols.
        /// </summary>
        /// <param name="character">The character to type</param>
        /// <returns>True if successful, false otherwise</returns>
        private bool TypeCharacter(char character)
        {
            // Key down
            var inputDown = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = character,
                        dwFlags = KEYEVENTF_UNICODE,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // Key up
            var inputUp = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = character,
                        dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            var inputs = new[] { inputDown, inputUp };
            uint result = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            
            // SendInput returns the number of events successfully inserted
            // Should be 2 (down + up) for success
            return result == 2;
        }

        /// <summary>
        /// Gets the current clipboard text content.
        /// </summary>
        /// <returns>Clipboard text or null if clipboard is empty or contains non-text data</returns>
        private string? GetClipboardText()
        {
            if (!OpenClipboard(IntPtr.Zero))
                return null;

            try
            {
                IntPtr hData = GetClipboardData(CF_UNICODETEXT);
                if (hData == IntPtr.Zero)
                    return null;

                IntPtr pData = GlobalLock(hData);
                if (pData == IntPtr.Zero)
                    return null;

                try
                {
                    return Marshal.PtrToStringUni(pData);
                }
                finally
                {
                    GlobalUnlock(hData);
                }
            }
            finally
            {
                CloseClipboard();
            }
        }

        /// <summary>
        /// Sets the clipboard to the specified text.
        /// </summary>
        /// <param name="text">The text to set</param>
        /// <returns>True if successful, false otherwise</returns>
        private bool SetClipboardText(string text)
        {
            if (!OpenClipboard(IntPtr.Zero))
                return false;

            try
            {
                if (!EmptyClipboard())
                    return false;

                // Allocate global memory for the text
                int byteCount = (text.Length + 1) * 2; // Unicode = 2 bytes per char + null terminator
                IntPtr hGlobal = GlobalAlloc(GHND, (UIntPtr)byteCount);
                if (hGlobal == IntPtr.Zero)
                    return false;

                try
                {
                    IntPtr pGlobal = GlobalLock(hGlobal);
                    if (pGlobal == IntPtr.Zero)
                    {
                        GlobalFree(hGlobal);
                        return false;
                    }

                    try
                    {
                        // Copy text to global memory
                        Marshal.Copy(text.ToCharArray(), 0, pGlobal, text.Length);
                        // Add null terminator
                        Marshal.WriteInt16(pGlobal, text.Length * 2, 0);
                    }
                    finally
                    {
                        GlobalUnlock(hGlobal);
                    }

                    // Set clipboard data
                    if (SetClipboardData(CF_UNICODETEXT, hGlobal) == IntPtr.Zero)
                    {
                        GlobalFree(hGlobal);
                        return false;
                    }

                    // Clipboard now owns the memory, don't free it
                    return true;
                }
                catch
                {
                    GlobalFree(hGlobal);
                    throw;
                }
            }
            finally
            {
                CloseClipboard();
            }
        }

        /// <summary>
        /// Clears the clipboard content.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        private bool ClearClipboard()
        {
            if (!OpenClipboard(IntPtr.Zero))
                return false;

            try
            {
                return EmptyClipboard();
            }
            finally
            {
                CloseClipboard();
            }
        }

        #endregion
    }
}
