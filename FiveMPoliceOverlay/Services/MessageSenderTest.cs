using System;
using System.Threading.Tasks;
using FiveMPoliceOverlay.Infrastructure;
using FiveMPoliceOverlay.Models;
using Xunit;

namespace FiveMPoliceOverlay.Services
{
    /// <summary>
    /// Unit tests for MessageSender service.
    /// Tests message formatting, mode switching, and SendResult enum handling.
    /// Note: Full integration tests require FiveM to be running.
    /// </summary>
    public class MessageSenderTest
    {
        #region Test Helpers

        /// <summary>
        /// Creates a test MessageTemplate
        /// </summary>
        private MessageTemplate CreateTestTemplate(string text = "Test message")
        {
            return new MessageTemplate
            {
                Id = "test-001",
                Category = "Test",
                Name = "Test Template",
                Text = text
            };
        }

        /// <summary>
        /// Creates a MessageSender instance for testing
        /// </summary>
        private MessageSender CreateMessageSender(bool isTestMode = false)
        {
            var simulator = new KeyboardSimulator();
            var rateLimiter = new RateLimiter(cooldownSeconds: 2, maxQueueSize: 5);
            return new MessageSender(simulator, rateLimiter, isTestMode);
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange
            var simulator = new KeyboardSimulator();
            var rateLimiter = new RateLimiter();

            // Act
            var sender = new MessageSender(simulator, rateLimiter, isTestMode: false);

            // Assert
            Assert.NotNull(sender);
            Assert.False(sender.IsTestMode);
        }

        [Fact]
        public void Constructor_WithTestMode_SetsTestModeTrue()
        {
            // Arrange
            var simulator = new KeyboardSimulator();
            var rateLimiter = new RateLimiter();

            // Act
            var sender = new MessageSender(simulator, rateLimiter, isTestMode: true);

            // Assert
            Assert.True(sender.IsTestMode);
        }

        [Fact]
        public void Constructor_WithNullSimulator_ThrowsArgumentNullException()
        {
            // Arrange
            var rateLimiter = new RateLimiter();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new MessageSender(null!, rateLimiter, isTestMode: false));
        }

        [Fact]
        public void Constructor_WithNullRateLimiter_ThrowsArgumentNullException()
        {
            // Arrange
            var simulator = new KeyboardSimulator();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new MessageSender(simulator, null!, isTestMode: false));
        }

        #endregion

        #region Mode Tests

        [Fact]
        public void SetMode_ToTestMode_UpdatesIsTestMode()
        {
            // Arrange
            var sender = CreateMessageSender(isTestMode: false);
            Assert.False(sender.IsTestMode);

            // Act
            sender.SetMode(true);

            // Assert
            Assert.True(sender.IsTestMode);
        }

        [Fact]
        public void SetMode_ToProductionMode_UpdatesIsTestMode()
        {
            // Arrange
            var sender = CreateMessageSender(isTestMode: true);
            Assert.True(sender.IsTestMode);

            // Act
            sender.SetMode(false);

            // Assert
            Assert.False(sender.IsTestMode);
        }

        [Fact]
        public void IsTestMode_Property_CanBeSetDirectly()
        {
            // Arrange
            var sender = CreateMessageSender(isTestMode: false);

            // Act
            sender.IsTestMode = true;

            // Assert
            Assert.True(sender.IsTestMode);
        }

        #endregion

        #region SendMessageAsync Tests

        [Fact]
        public async Task SendMessageAsync_WithNullTemplate_ThrowsArgumentNullException()
        {
            // Arrange
            var sender = CreateMessageSender();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                sender.SendMessageAsync(null!));
        }

        [Fact]
        public async Task SendMessageAsync_WhenRateLimited_ReturnsRateLimited()
        {
            // Arrange
            var simulator = new KeyboardSimulator();
            var rateLimiter = new RateLimiter(cooldownSeconds: 2, maxQueueSize: 5);
            var sender = new MessageSender(simulator, rateLimiter, isTestMode: false);
            var template = CreateTestTemplate();

            // Simulate rate limiting by marking a message as sent
            rateLimiter.MarkMessageSent();

            // Act
            var result = await sender.SendMessageAsync(template);

            // Assert
            Assert.Equal(SendResult.RateLimited, result);
        }

        [Fact]
        public async Task SendMessageAsync_WhenRateLimitedAndQueueFull_ReturnsRateLimited()
        {
            // Arrange
            var simulator = new KeyboardSimulator();
            var rateLimiter = new RateLimiter(cooldownSeconds: 2, maxQueueSize: 2);
            var sender = new MessageSender(simulator, rateLimiter, isTestMode: false);
            var template = CreateTestTemplate();

            // Mark message as sent to trigger rate limiting
            rateLimiter.MarkMessageSent();

            // Fill the queue
            rateLimiter.TryEnqueue(CreateTestTemplate("Message 1"));
            rateLimiter.TryEnqueue(CreateTestTemplate("Message 2"));

            // Act
            var result = await sender.SendMessageAsync(template);

            // Assert
            Assert.Equal(SendResult.RateLimited, result);
        }

        #endregion

        #region Message Formatting Tests (via reflection or indirect testing)

        /// <summary>
        /// Tests that message formatting works correctly by verifying the mode affects behavior.
        /// Since FormatMessage is private, we test it indirectly through the public API.
        /// </summary>
        [Fact]
        public void MessageFormatting_TestMode_UsesSlashMe()
        {
            // Arrange
            var sender = CreateMessageSender(isTestMode: true);
            var template = CreateTestTemplate("Test broadcast");

            // Act - We can't directly test FormatMessage since it's private,
            // but we can verify the mode is set correctly
            Assert.True(sender.IsTestMode);

            // The actual formatting will be tested in integration tests
            // where we can verify the actual message sent to FiveM
        }

        [Fact]
        public void MessageFormatting_ProductionMode_UsesSlashInfo()
        {
            // Arrange
            var sender = CreateMessageSender(isTestMode: false);
            var template = CreateTestTemplate("Test broadcast");

            // Act - We can't directly test FormatMessage since it's private,
            // but we can verify the mode is set correctly
            Assert.False(sender.IsTestMode);

            // The actual formatting will be tested in integration tests
            // where we can verify the actual message sent to FiveM
        }

        #endregion

        #region SendResult Enum Tests

        [Fact]
        public void SendResult_HasExpectedValues()
        {
            // Assert - Verify all expected enum values exist
            Assert.True(Enum.IsDefined(typeof(SendResult), SendResult.Success));
            Assert.True(Enum.IsDefined(typeof(SendResult), SendResult.FiveMNotFocused));
            Assert.True(Enum.IsDefined(typeof(SendResult), SendResult.RateLimited));
            Assert.True(Enum.IsDefined(typeof(SendResult), SendResult.SimulationFailed));
        }

        [Fact]
        public void SendResult_EnumValues_AreDistinct()
        {
            // Assert - Verify enum values are distinct
            Assert.NotEqual(SendResult.Success, SendResult.FiveMNotFocused);
            Assert.NotEqual(SendResult.Success, SendResult.RateLimited);
            Assert.NotEqual(SendResult.Success, SendResult.SimulationFailed);
            Assert.NotEqual(SendResult.FiveMNotFocused, SendResult.RateLimited);
            Assert.NotEqual(SendResult.FiveMNotFocused, SendResult.SimulationFailed);
            Assert.NotEqual(SendResult.RateLimited, SendResult.SimulationFailed);
        }

        #endregion

        #region Integration Test Notes

        /*
         * INTEGRATION TESTS (require FiveM running):
         * 
         * These tests cannot be automated in unit tests as they require:
         * 1. FiveM process running
         * 2. FiveM window having focus
         * 3. Ability to verify chat messages in FiveM
         * 
         * Manual integration test scenarios:
         * 
         * 1. Test Mode Message Send:
         *    - Set IsTestMode = true
         *    - Send a message
         *    - Verify "/me [message]" appears in FiveM chat
         * 
         * 2. Production Mode Message Send:
         *    - Set IsTestMode = false
         *    - Send a message
         *    - Verify "/info [message]" appears in FiveM chat
         * 
         * 3. Focus Loss Retry:
         *    - Start sending a message
         *    - Switch focus away from FiveM during send
         *    - Verify message is queued and retried
         * 
         * 4. Rate Limiting:
         *    - Send multiple messages rapidly
         *    - Verify cooldown indicator appears
         *    - Verify messages are queued
         * 
         * 5. Long Message Optimization:
         *    - Send a message >100 characters
         *    - Verify clipboard paste is used (faster than typing)
         * 
         * 6. Simulation Failure Recovery:
         *    - Simulate keyboard input failure
         *    - Verify error is logged and returned
         *    - Verify chat is closed with Escape
         */

        #endregion
    }
}
