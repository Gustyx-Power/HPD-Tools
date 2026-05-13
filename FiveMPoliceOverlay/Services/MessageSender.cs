using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FiveMPoliceOverlay.Infrastructure;
using FiveMPoliceOverlay.Models;

namespace FiveMPoliceOverlay.Services
{
    /// <summary>
    /// Result of a message send operation
    /// </summary>
    public enum SendResult
    {
        Success,
        FiveMNotFocused,
        RateLimited,
        SimulationFailed
    }

    /// <summary>
    /// Service responsible for sending messages to FiveM chat.
    /// Integrates KeyboardSimulator and RateLimiter to format and send messages.
    /// Handles FiveM focus verification, retry logic, and error handling.
    /// </summary>
    public class MessageSender
    {
        #region Windows API P/Invoke

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        #endregion

        #region Fields

        private readonly KeyboardSimulator _keyboardSimulator;
        private readonly RateLimiter _rateLimiter;
        private bool _isTestMode;
        private const int MaxRetries = 3;
        private const int ChatOpenDelayMs = 350;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of MessageSender.
        /// </summary>
        /// <param name="keyboardSimulator">Keyboard simulator for input simulation</param>
        /// <param name="rateLimiter">Rate limiter for cooldown management</param>
        /// <param name="isTestMode">Whether to use test mode (/me) or production mode (/info)</param>
        public MessageSender(KeyboardSimulator keyboardSimulator, RateLimiter rateLimiter, bool isTestMode = false)
        {
            _keyboardSimulator = keyboardSimulator ?? throw new ArgumentNullException(nameof(keyboardSimulator));
            _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
            _isTestMode = isTestMode;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets whether test mode is enabled.
        /// Test mode uses /me command, production mode uses /info command.
        /// </summary>
        public bool IsTestMode
        {
            get => _isTestMode;
            set => _isTestMode = value;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sends a message to FiveM chat asynchronously.
        /// Checks rate limiting, verifies FiveM focus, formats message, and simulates keyboard input.
        /// </summary>
        /// <param name="template">The message template to send</param>
        /// <returns>SendResult indicating success or failure reason</returns>
        public async Task<SendResult> SendMessageAsync(MessageTemplate template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            // Check rate limiting
            if (_rateLimiter.IsRateLimited())
            {
                Console.WriteLine($"[MessageSender] Rate limited, cooldown remaining: {_rateLimiter.GetCooldownRemaining().TotalSeconds:F1}s");
                
                // Try to enqueue message for later
                if (_rateLimiter.TryEnqueue(template))
                {
                    Console.WriteLine($"[MessageSender] Message queued: {template.Name}");
                }
                else
                {
                    Console.WriteLine($"[MessageSender] Queue full, message dropped: {template.Name}");
                }
                
                return SendResult.RateLimited;
            }

            // Format message based on mode
            string formattedMessage = FormatMessage(template);
            Console.WriteLine($"[MessageSender] Sending message: {formattedMessage}");

            // Try to send with retry logic
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                // Verify FiveM has focus
                if (!IsFiveMFocused())
                {
                    Console.WriteLine($"[MessageSender] FiveM not focused (attempt {attempt}/{MaxRetries})");
                    
                    if (attempt < MaxRetries)
                    {
                        // Wait before retry
                        await Task.Delay(500);
                        continue;
                    }
                    else
                    {
                        // Max retries reached, queue message
                        Console.WriteLine($"[MessageSender] Max retries reached, queueing message");
                        _rateLimiter.TryEnqueue(template);
                        return SendResult.FiveMNotFocused;
                    }
                }

                // Send the message
                SendResult result = await SendMessageInternalAsync(formattedMessage);
                
                if (result == SendResult.Success)
                {
                    // Mark message as sent (starts cooldown)
                    _rateLimiter.MarkMessageSent();
                    Console.WriteLine($"[MessageSender] Message sent successfully: {template.Name}");
                    return SendResult.Success;
                }
                else if (result == SendResult.FiveMNotFocused)
                {
                    // Focus lost during send, retry
                    Console.WriteLine($"[MessageSender] Focus lost during send (attempt {attempt}/{MaxRetries})");
                    
                    if (attempt < MaxRetries)
                    {
                        await Task.Delay(500);
                        continue;
                    }
                    else
                    {
                        // Max retries reached, queue message
                        _rateLimiter.TryEnqueue(template);
                        return SendResult.FiveMNotFocused;
                    }
                }
                else
                {
                    // Simulation failed
                    Console.WriteLine($"[MessageSender] Simulation failed (attempt {attempt}/{MaxRetries})");
                    
                    if (attempt < MaxRetries)
                    {
                        await Task.Delay(500);
                        continue;
                    }
                    else
                    {
                        return SendResult.SimulationFailed;
                    }
                }
            }

            // Should not reach here, but return failure as fallback
            return SendResult.SimulationFailed;
        }

        /// <summary>
        /// Sets the test mode flag.
        /// </summary>
        /// <param name="isTestMode">True for test mode (/me), false for production mode (/info)</param>
        public void SetMode(bool isTestMode)
        {
            _isTestMode = isTestMode;
            Console.WriteLine($"[MessageSender] Mode changed to: {(_isTestMode ? "Test (/me)" : "Production (/info)")}");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Formats a message template based on current mode.
        /// Test mode: /me [message]
        /// Production mode: /info [message]
        /// </summary>
        /// <param name="template">The message template to format</param>
        /// <returns>Formatted message string</returns>
        private string FormatMessage(MessageTemplate template)
        {
            string command = _isTestMode ? "/me" : "/info";
            return $"{command} {template.Text}";
        }

        /// <summary>
        /// Checks if FiveM window currently has focus.
        /// </summary>
        /// <returns>True if FiveM is focused, false otherwise</returns>
        private bool IsFiveMFocused()
        {
            try
            {
                // Get foreground window
                IntPtr foregroundWindow = GetForegroundWindow();
                if (foregroundWindow == IntPtr.Zero)
                {
                    return false;
                }

                // Get process ID of foreground window
                GetWindowThreadProcessId(foregroundWindow, out uint processId);
                if (processId == 0)
                {
                    return false;
                }

                // Get process name
                try
                {
                    var process = Process.GetProcessById((int)processId);
                    string processName = process.ProcessName.ToLowerInvariant();
                    
                    // Check if it's FiveM or any version of FiveM_GTAProcess (e.g. fivem_b2944_gtaprocess)
                    return processName == "fivem" || (processName.StartsWith("fivem") && processName.Contains("gtaprocess"));
                }
                catch (ArgumentException)
                {
                    // Process not found
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MessageSender] Error checking FiveM focus: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Internal method to send a formatted message to FiveM chat.
        /// Opens chat, types/pastes message, and sends it.
        /// </summary>
        /// <param name="formattedMessage">The formatted message to send</param>
        /// <returns>SendResult indicating success or failure</returns>
        private async Task<SendResult> SendMessageInternalAsync(string formattedMessage)
        {
            try
            {
                // Step 1: Open chat with T key
                if (!_keyboardSimulator.PressKey(KeyboardSimulator.VirtualKeyCode.VK_T))
                {
                    Console.WriteLine("[MessageSender] Failed to press T key to open chat");
                    return SendResult.SimulationFailed;
                }

                // Step 2: Wait for chat to open
                await Task.Delay(ChatOpenDelayMs);

                // Step 3: Verify FiveM still has focus
                if (!IsFiveMFocused())
                {
                    Console.WriteLine("[MessageSender] FiveM lost focus after opening chat");
                    
                    // Try to close chat with Escape
                    _keyboardSimulator.PressKey(KeyboardSimulator.VirtualKeyCode.ESCAPE);
                    
                    return SendResult.FiveMNotFocused;
                }

                // Step 4: Type/paste the message
                if (!_keyboardSimulator.TypeTextOptimized(formattedMessage))
                {
                    Console.WriteLine("[MessageSender] Failed to type message");
                    
                    // Try to close chat with Escape
                    _keyboardSimulator.PressKey(KeyboardSimulator.VirtualKeyCode.ESCAPE);
                    
                    return SendResult.SimulationFailed;
                }

                // Wait briefly for text processing to catch up in game UI
                await Task.Delay(100);

                // Step 5: Send message with Enter key
                if (!_keyboardSimulator.PressKey(KeyboardSimulator.VirtualKeyCode.RETURN))
                {
                    Console.WriteLine("[MessageSender] Failed to press Enter key to send message");
                    
                    // Try to close chat with Escape
                    _keyboardSimulator.PressKey(KeyboardSimulator.VirtualKeyCode.ESCAPE);
                    
                    return SendResult.SimulationFailed;
                }

                return SendResult.Success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MessageSender] Error during message send: {ex.Message}");
                return SendResult.SimulationFailed;
            }
        }

        #endregion
    }
}
