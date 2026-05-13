using System;
using System.Threading.Tasks;
using FiveMPoliceOverlay.Models;

namespace FiveMPoliceOverlay.Services
{
    /// <summary>
    /// Integrates KeybindManager with MessageSender to create a complete pipeline:
    /// Keybind Press → MessageSender → FiveM Chat
    /// Provides feedback callbacks for success and error scenarios.
    /// </summary>
    public class KeybindMessageIntegration : IDisposable
    {
        #region Fields

        private readonly KeybindManager _keybindManager;
        private readonly MessageSender _messageSender;
        private bool _isDisposed;

        #endregion

        #region Events

        /// <summary>
        /// Fired when a message is sent successfully.
        /// </summary>
        public event EventHandler<MessageSentEventArgs>? MessageSentSuccessfully;

        /// <summary>
        /// Fired when a message send operation fails.
        /// </summary>
        public event EventHandler<MessageSendErrorEventArgs>? MessageSendFailed;

        /// <summary>
        /// Fired when a message is rate limited.
        /// </summary>
        public event EventHandler<MessageRateLimitedEventArgs>? MessageRateLimited;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of KeybindMessageIntegration.
        /// </summary>
        /// <param name="keybindManager">The keybind manager to listen to</param>
        /// <param name="messageSender">The message sender to use for sending messages</param>
        public KeybindMessageIntegration(KeybindManager keybindManager, MessageSender messageSender)
        {
            _keybindManager = keybindManager ?? throw new ArgumentNullException(nameof(keybindManager));
            _messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));

            // Wire up the KeybindPressed event
            _keybindManager.KeybindPressed += OnKeybindPressed;

            Console.WriteLine("[KeybindMessageIntegration] Integration initialized");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles the KeybindPressed event from KeybindManager.
        /// Sends the message via MessageSender and provides appropriate feedback.
        /// </summary>
        private async void OnKeybindPressed(object? sender, KeybindPressedEventArgs e)
        {
            try
            {
                Console.WriteLine($"[KeybindMessageIntegration] Keybind pressed, sending message: {e.Template.Name}");

                // Send the message asynchronously
                SendResult result = await _messageSender.SendMessageAsync(e.Template);

                // Handle the result and provide feedback
                switch (result)
                {
                    case SendResult.Success:
                        Console.WriteLine($"[KeybindMessageIntegration] Message sent successfully: {e.Template.Name}");
                        OnMessageSentSuccessfully(e.Template);
                        break;

                    case SendResult.RateLimited:
                        Console.WriteLine($"[KeybindMessageIntegration] Message rate limited: {e.Template.Name}");
                        OnMessageRateLimited(e.Template);
                        break;

                    case SendResult.FiveMNotFocused:
                        Console.WriteLine($"[KeybindMessageIntegration] Message failed - FiveM not focused: {e.Template.Name}");
                        OnMessageSendFailed(e.Template, "FiveM window tidak aktif. Pesan akan dikirim saat FiveM aktif kembali.");
                        break;

                    case SendResult.SimulationFailed:
                        Console.WriteLine($"[KeybindMessageIntegration] Message failed - simulation error: {e.Template.Name}");
                        OnMessageSendFailed(e.Template, "Gagal mengirim pesan. Pastikan FiveM berjalan dengan benar.");
                        break;

                    default:
                        Console.WriteLine($"[KeybindMessageIntegration] Unknown send result: {result}");
                        OnMessageSendFailed(e.Template, "Terjadi kesalahan tidak dikenal saat mengirim pesan.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KeybindMessageIntegration] Exception during message send: {ex.Message}");
                OnMessageSendFailed(e.Template, $"Terjadi kesalahan: {ex.Message}");
            }
        }

        /// <summary>
        /// Raises the MessageSentSuccessfully event.
        /// </summary>
        protected virtual void OnMessageSentSuccessfully(MessageTemplate template)
        {
            MessageSentSuccessfully?.Invoke(this, new MessageSentEventArgs(template));
        }

        /// <summary>
        /// Raises the MessageSendFailed event.
        /// </summary>
        protected virtual void OnMessageSendFailed(MessageTemplate template, string errorMessage)
        {
            MessageSendFailed?.Invoke(this, new MessageSendErrorEventArgs(template, errorMessage));
        }

        /// <summary>
        /// Raises the MessageRateLimited event.
        /// </summary>
        protected virtual void OnMessageRateLimited(MessageTemplate template)
        {
            MessageRateLimited?.Invoke(this, new MessageRateLimitedEventArgs(template));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_isDisposed)
            {
                // Unwire the event
                _keybindManager.KeybindPressed -= OnKeybindPressed;

                _isDisposed = true;
                Console.WriteLine("[KeybindMessageIntegration] Integration disposed");
            }
        }

        #endregion
    }

    #region Event Args Classes

    /// <summary>
    /// Event arguments for MessageSentSuccessfully event.
    /// </summary>
    public class MessageSentEventArgs : EventArgs
    {
        public MessageTemplate Template { get; }

        public MessageSentEventArgs(MessageTemplate template)
        {
            Template = template ?? throw new ArgumentNullException(nameof(template));
        }
    }

    /// <summary>
    /// Event arguments for MessageSendFailed event.
    /// </summary>
    public class MessageSendErrorEventArgs : EventArgs
    {
        public MessageTemplate Template { get; }
        public string ErrorMessage { get; }

        public MessageSendErrorEventArgs(MessageTemplate template, string errorMessage)
        {
            Template = template ?? throw new ArgumentNullException(nameof(template));
            ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
        }
    }

    /// <summary>
    /// Event arguments for MessageRateLimited event.
    /// </summary>
    public class MessageRateLimitedEventArgs : EventArgs
    {
        public MessageTemplate Template { get; }

        public MessageRateLimitedEventArgs(MessageTemplate template)
        {
            Template = template ?? throw new ArgumentNullException(nameof(template));
        }
    }

    #endregion
}
