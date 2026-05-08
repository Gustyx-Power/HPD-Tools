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

        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_UNICODE = 0x0004;
        private const uint KEYEVENTF_SCANCODE = 0x0008;

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

        #endregion
    }
}
