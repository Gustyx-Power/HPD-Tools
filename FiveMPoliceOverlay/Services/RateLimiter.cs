using System;
using System.Collections.Generic;
using FiveMPoliceOverlay.Models;

namespace FiveMPoliceOverlay.Services
{
    /// <summary>
    /// Manages rate limiting and message queuing to prevent spam.
    /// Enforces a 2-second cooldown between messages and maintains a queue of up to 5 pending messages.
    /// </summary>
    public class RateLimiter
    {
        #region Fields

        private readonly Queue<MessageTemplate> _messageQueue;
        private readonly int _cooldownSeconds;
        private readonly int _maxQueueSize;
        private DateTime _lastMessageSentTime;
        private readonly object _lock = new object();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of RateLimiter with default settings (2 second cooldown, 5 max queue size).
        /// </summary>
        public RateLimiter() : this(2, 5)
        {
        }

        /// <summary>
        /// Initializes a new instance of RateLimiter with custom settings.
        /// </summary>
        /// <param name="cooldownSeconds">Cooldown period in seconds between messages</param>
        /// <param name="maxQueueSize">Maximum number of messages that can be queued</param>
        public RateLimiter(int cooldownSeconds, int maxQueueSize)
        {
            if (cooldownSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(cooldownSeconds), "Cooldown seconds must be non-negative");
            if (maxQueueSize < 0)
                throw new ArgumentOutOfRangeException(nameof(maxQueueSize), "Max queue size must be non-negative");

            _cooldownSeconds = cooldownSeconds;
            _maxQueueSize = maxQueueSize;
            _messageQueue = new Queue<MessageTemplate>(maxQueueSize);
            _lastMessageSentTime = DateTime.MinValue;
        }

        /// <summary>
        /// Initializes a new instance of RateLimiter from configuration settings.
        /// </summary>
        /// <param name="settings">Rate limit settings from configuration</param>
        public RateLimiter(RateLimitSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            _cooldownSeconds = settings.CooldownSeconds;
            _maxQueueSize = settings.MaxQueueSize;
            _messageQueue = new Queue<MessageTemplate>(_maxQueueSize);
            _lastMessageSentTime = DateTime.MinValue;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the cooldown period in seconds.
        /// </summary>
        public int CooldownSeconds => _cooldownSeconds;

        /// <summary>
        /// Gets the maximum queue size.
        /// </summary>
        public int MaxQueueSize => _maxQueueSize;

        /// <summary>
        /// Gets the current number of messages in the queue.
        /// </summary>
        public int QueueCount
        {
            get
            {
                lock (_lock)
                {
                    return _messageQueue.Count;
                }
            }
        }

        /// <summary>
        /// Gets the time when the last message was sent.
        /// </summary>
        public DateTime LastMessageSentTime
        {
            get
            {
                lock (_lock)
                {
                    return _lastMessageSentTime;
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Checks if the rate limiter is currently in cooldown period.
        /// </summary>
        /// <returns>True if rate limited (cooldown active), false if ready to send</returns>
        public bool IsRateLimited()
        {
            lock (_lock)
            {
                return GetCooldownRemaining() > TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Gets the remaining cooldown time.
        /// </summary>
        /// <returns>TimeSpan representing remaining cooldown, or TimeSpan.Zero if not rate limited</returns>
        public TimeSpan GetCooldownRemaining()
        {
            lock (_lock)
            {
                if (_lastMessageSentTime == DateTime.MinValue)
                {
                    return TimeSpan.Zero;
                }

                var elapsed = DateTime.UtcNow - _lastMessageSentTime;
                var cooldownPeriod = TimeSpan.FromSeconds(_cooldownSeconds);

                if (elapsed >= cooldownPeriod)
                {
                    return TimeSpan.Zero;
                }

                return cooldownPeriod - elapsed;
            }
        }

        /// <summary>
        /// Attempts to enqueue a message for later sending.
        /// </summary>
        /// <param name="template">The message template to enqueue</param>
        /// <returns>True if message was enqueued, false if queue is full</returns>
        public bool TryEnqueue(MessageTemplate template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            lock (_lock)
            {
                if (_messageQueue.Count >= _maxQueueSize)
                {
                    Console.WriteLine($"[RateLimiter] Queue full, dropping message: {template.Name}");
                    return false;
                }

                _messageQueue.Enqueue(template);
                Console.WriteLine($"[RateLimiter] Message enqueued: {template.Name} (Queue: {_messageQueue.Count}/{_maxQueueSize})");
                return true;
            }
        }

        /// <summary>
        /// Attempts to dequeue the next message from the queue.
        /// </summary>
        /// <param name="template">The dequeued message template, or null if queue is empty</param>
        /// <returns>True if a message was dequeued, false if queue is empty</returns>
        public bool TryDequeue(out MessageTemplate? template)
        {
            lock (_lock)
            {
                if (_messageQueue.Count == 0)
                {
                    template = null;
                    return false;
                }

                template = _messageQueue.Dequeue();
                Console.WriteLine($"[RateLimiter] Message dequeued: {template.Name} (Queue: {_messageQueue.Count}/{_maxQueueSize})");
                return true;
            }
        }

        /// <summary>
        /// Marks that a message has been sent, updating the last sent time.
        /// This starts the cooldown period.
        /// </summary>
        public void MarkMessageSent()
        {
            lock (_lock)
            {
                _lastMessageSentTime = DateTime.UtcNow;
                Console.WriteLine($"[RateLimiter] Message sent, cooldown started ({_cooldownSeconds}s)");
            }
        }

        /// <summary>
        /// Clears all messages from the queue.
        /// </summary>
        public void ClearQueue()
        {
            lock (_lock)
            {
                int count = _messageQueue.Count;
                _messageQueue.Clear();
                Console.WriteLine($"[RateLimiter] Queue cleared ({count} messages removed)");
            }
        }

        /// <summary>
        /// Resets the rate limiter, clearing the queue and cooldown.
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _messageQueue.Clear();
                _lastMessageSentTime = DateTime.MinValue;
                Console.WriteLine("[RateLimiter] Rate limiter reset");
            }
        }

        #endregion
    }
}
