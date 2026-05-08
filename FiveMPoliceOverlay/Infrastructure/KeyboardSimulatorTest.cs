using System;
using System.Diagnostics;
using System.Threading;
using Xunit;

namespace FiveMPoliceOverlay.Infrastructure
{
    /// <summary>
    /// Unit tests for KeyboardSimulator service.
    /// Note: These tests verify the API calls succeed, but cannot verify actual keyboard input
    /// without a target application. Manual testing with Notepad is recommended.
    /// </summary>
    public class KeyboardSimulatorTest
    {
        private readonly KeyboardSimulator _simulator;

        public KeyboardSimulatorTest()
        {
            _simulator = new KeyboardSimulator();
        }

        [Fact]
        public void TypeText_WithEmptyString_ReturnsTrue()
        {
            // Arrange
            string emptyText = "";

            // Act
            bool result = _simulator.TypeText(emptyText);

            // Assert
            Assert.True(result, "TypeText should return true for empty string");
        }

        [Fact]
        public void TypeText_WithNullString_ReturnsTrue()
        {
            // Arrange
            string? nullText = null;

            // Act
            bool result = _simulator.TypeText(nullText!);

            // Assert
            Assert.True(result, "TypeText should return true for null string");
        }

        [Fact]
        public void TypeText_WithSimpleText_ReturnsTrue()
        {
            // Arrange
            string simpleText = "Hello";

            // Act
            bool result = _simulator.TypeText(simpleText);

            // Assert
            Assert.True(result, "TypeText should return true for simple text");
        }

        [Fact]
        public void TypeText_WithIndonesianText_ReturnsTrue()
        {
            // Arrange
            string indonesianText = "DI INFORMASIKAN KEPADA SELURUH WARGA";

            // Act
            bool result = _simulator.TypeText(indonesianText);

            // Assert
            Assert.True(result, "TypeText should return true for Indonesian text");
        }

        [Fact]
        public void TypeText_WithSpecialCharacters_ReturnsTrue()
        {
            // Arrange
            string specialText = "Test: 123 @#$%";

            // Act
            bool result = _simulator.TypeText(specialText);

            // Assert
            Assert.True(result, "TypeText should return true for text with special characters");
        }

        [Fact]
        public void PressKey_WithEnterKey_ReturnsTrue()
        {
            // Arrange
            var keyCode = KeyboardSimulator.VirtualKeyCode.RETURN;

            // Act
            bool result = _simulator.PressKey(keyCode);

            // Assert
            Assert.True(result, "PressKey should return true for Enter key");
        }

        [Fact]
        public void PressKey_WithTKey_ReturnsTrue()
        {
            // Arrange
            var keyCode = KeyboardSimulator.VirtualKeyCode.VK_T;

            // Act
            bool result = _simulator.PressKey(keyCode);

            // Assert
            Assert.True(result, "PressKey should return true for T key");
        }

        [Fact]
        public void KeyDown_WithControlKey_ReturnsTrue()
        {
            // Arrange
            var keyCode = KeyboardSimulator.VirtualKeyCode.CONTROL;

            // Act
            bool result = _simulator.KeyDown(keyCode);

            // Assert
            Assert.True(result, "KeyDown should return true for Control key");
            
            // Cleanup: Release the key
            _simulator.KeyUp(keyCode);
        }

        [Fact]
        public void KeyUp_WithControlKey_ReturnsTrue()
        {
            // Arrange
            var keyCode = KeyboardSimulator.VirtualKeyCode.CONTROL;
            _simulator.KeyDown(keyCode); // Press down first

            // Act
            bool result = _simulator.KeyUp(keyCode);

            // Assert
            Assert.True(result, "KeyUp should return true for Control key");
        }

        [Fact]
        public void KeyDown_And_KeyUp_Sequence_ReturnsTrue()
        {
            // Arrange
            var keyCode = KeyboardSimulator.VirtualKeyCode.SHIFT;

            // Act
            bool downResult = _simulator.KeyDown(keyCode);
            Thread.Sleep(50);
            bool upResult = _simulator.KeyUp(keyCode);

            // Assert
            Assert.True(downResult, "KeyDown should return true");
            Assert.True(upResult, "KeyUp should return true");
        }

        [Fact]
        public void PressKey_MultipleTimes_ReturnsTrue()
        {
            // Arrange
            var keyCode = KeyboardSimulator.VirtualKeyCode.SPACE;

            // Act & Assert
            for (int i = 0; i < 3; i++)
            {
                bool result = _simulator.PressKey(keyCode);
                Assert.True(result, $"PressKey should return true on iteration {i + 1}");
                Thread.Sleep(100);
            }
        }

        [Fact]
        public void TypeText_WithLongMessage_ReturnsTrue()
        {
            // Arrange
            string longMessage = "DI INFORMASIKAN KEPADA SELURUH WARGA AGAR SEGERA MENGAMANKAN DIRI DAN MENCARI TEMPAT AMAN DIKARENAKAN KOTA MENGALAMI SIAGA III TERIMAKASIH.";

            // Act
            bool result = _simulator.TypeText(longMessage);

            // Assert
            Assert.True(result, "TypeText should return true for long message (predefined template)");
        }

        /// <summary>
        /// Manual test helper: Simulates opening FiveM chat and sending a message.
        /// This test is marked as skipped because it requires FiveM to be running.
        /// To run manually: Remove [Fact(Skip = ...)] and replace with [Fact]
        /// </summary>
        [Fact(Skip = "Manual test - requires FiveM running and focused")]
        public void ManualTest_SendMessageToFiveM()
        {
            // This test simulates the actual message sending flow
            // 1. Press T to open chat
            // 2. Type message
            // 3. Press Enter to send

            Console.WriteLine("Starting manual test in 3 seconds...");
            Console.WriteLine("Please focus FiveM window now!");
            Thread.Sleep(3000);

            // Open chat
            bool chatOpened = _simulator.PressKey(KeyboardSimulator.VirtualKeyCode.VK_T);
            Assert.True(chatOpened, "Failed to open chat");
            Thread.Sleep(50);

            // Type test message
            string testMessage = "/me Testing keyboard simulator";
            bool messageTyped = _simulator.TypeText(testMessage);
            Assert.True(messageTyped, "Failed to type message");

            // Send message
            bool messageSent = _simulator.PressKey(KeyboardSimulator.VirtualKeyCode.RETURN);
            Assert.True(messageSent, "Failed to send message");

            Console.WriteLine("Manual test completed. Check FiveM chat for message.");
        }
    }
}
