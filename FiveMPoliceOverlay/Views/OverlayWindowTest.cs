using System;
using System.Threading;
using System.Windows;

namespace FiveMPoliceOverlay.Views
{
    /// <summary>
    /// Manual tests for OverlayWindow to verify window properties and basic functionality.
    /// Note: These tests require STA thread for WPF components.
    /// Call RunTests() from your application to execute tests.
    /// </summary>
    public class OverlayWindowTest
    {
        #region Test Helpers

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Assertion failed: {message}");
            }
        }

        #endregion

        #region Main Test Runner

        public static void RunTests()
        {
            Console.WriteLine("=== OverlayWindow Test Suite ===\n");

            try
            {
                TestWindowProperties();
                TestDefaultPosition();
                TestDefaultSize();
                TestSetMode();
                TestUpdatePosition();
                TestShowSuccessFeedback();
                TestShowCooldown();
                TestHideCooldown();
                TestCustomPosition();
                TestPositionChangedEvent();

                Console.WriteLine("\n=== All OverlayWindow Tests Completed Successfully ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[ERROR] Test failed: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        #endregion

        #region Window Properties Tests

        private static void TestWindowProperties()
        {
            Console.WriteLine("Test 1: Window Properties");
            Console.WriteLine("-------------------------");

            OverlayWindow? window = null;
            Exception? exception = null;

            var thread = new Thread(() =>
            {
                try
                {
                    window = new OverlayWindow();
                    
                    Assert(window.WindowStyle == WindowStyle.None, "WindowStyle should be None");
                    Assert(window.AllowsTransparency == true, "AllowsTransparency should be true");
                    Assert(window.Topmost == true, "Topmost should be true");
                    Assert(window.ShowInTaskbar == false, "ShowInTaskbar should be false");
                    Assert(window.ResizeMode == ResizeMode.NoResize, "ResizeMode should be NoResize");
                    
                    Console.WriteLine("  ✓ WindowStyle = None");
                    Console.WriteLine("  ✓ AllowsTransparency = true");
                    Console.WriteLine("  ✓ Topmost = true");
                    Console.WriteLine("  ✓ ShowInTaskbar = false");
                    Console.WriteLine("  ✓ ResizeMode = NoResize");
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (exception != null)
            {
                throw exception;
            }

            Console.WriteLine("✓ Window properties test passed\n");
        }

        #endregion

        #region Default Position Tests

        private static void TestDefaultPosition()
        {
            Console.WriteLine("Test 2: Default Position");
            Console.WriteLine("------------------------");

            OverlayWindow? window = null;

            var thread = new Thread(() =>
            {
                window = new OverlayWindow();
                
                Assert(window.Left == 10, "Default Left position should be 10");
                Assert(window.Top == 100, "Default Top position should be 100");
                
                Console.WriteLine($"  ✓ Left = {window.Left}");
                Console.WriteLine($"  ✓ Top = {window.Top}");
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Console.WriteLine("✓ Default position test passed\n");
        }

        #endregion

        #region Default Size Tests

        private static void TestDefaultSize()
        {
            Console.WriteLine("Test 3: Default Size");
            Console.WriteLine("--------------------");

            OverlayWindow? window = null;

            var thread = new Thread(() =>
            {
                window = new OverlayWindow();
                
                Assert(window.Width == 50, "Default Width should be 50");
                Assert(window.Height == 200, "Default Height should be 200");
                
                Console.WriteLine($"  ✓ Width = {window.Width}");
                Console.WriteLine($"  ✓ Height = {window.Height}");
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Console.WriteLine("✓ Default size test passed\n");
        }

        #endregion

        #region Set Mode Tests

        private static void TestSetMode()
        {
            Console.WriteLine("Test 4: Set Mode");
            Console.WriteLine("----------------");

            var thread = new Thread(() =>
            {
                var window = new OverlayWindow();

                // Test production mode (default)
                window.SetMode(false);
                Assert(window.ModeText.Text == "PROD", "Mode text should be 'PROD' for production mode");
                Console.WriteLine("  ✓ Production mode: PROD");

                // Test test mode
                window.SetMode(true);
                Assert(window.ModeText.Text == "TEST", "Mode text should be 'TEST' for test mode");
                Console.WriteLine("  ✓ Test mode: TEST");

                // Switch back to production
                window.SetMode(false);
                Assert(window.ModeText.Text == "PROD", "Mode text should be 'PROD' after switching back");
                Console.WriteLine("  ✓ Switch back to production: PROD");
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Console.WriteLine("✓ Set mode test passed\n");
        }

        #endregion

        #region Update Position Tests

        private static void TestUpdatePosition()
        {
            Console.WriteLine("Test 5: Update Position");
            Console.WriteLine("-----------------------");

            var thread = new Thread(() =>
            {
                var window = new OverlayWindow();

                // Test updating position
                window.UpdatePosition(100, 200);
                Assert(window.Left == 100, "Left should be 100 after update");
                Assert(window.Top == 200, "Top should be 200 after update");
                Console.WriteLine($"  ✓ Position updated to ({window.Left}, {window.Top})");

                // Test updating to different position
                window.UpdatePosition(50, 75);
                Assert(window.Left == 50, "Left should be 50 after second update");
                Assert(window.Top == 75, "Top should be 75 after second update");
                Console.WriteLine($"  ✓ Position updated to ({window.Left}, {window.Top})");
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Console.WriteLine("✓ Update position test passed\n");
        }

        #endregion

        #region Show Success Feedback Tests

        private static void TestShowSuccessFeedback()
        {
            Console.WriteLine("Test 6: Show Success Feedback");
            Console.WriteLine("------------------------------");

            var thread = new Thread(() =>
            {
                var window = new OverlayWindow();

                // Initially hidden
                Assert(window.SuccessIndicator.Visibility == Visibility.Collapsed, 
                    "Success indicator should be collapsed initially");
                Console.WriteLine("  ✓ Success indicator initially collapsed");

                // Show success feedback
                window.ShowSuccessFeedback();
                Assert(window.SuccessIndicator.Visibility == Visibility.Visible, 
                    "Success indicator should be visible after ShowSuccessFeedback");
                Console.WriteLine("  ✓ Success indicator visible after ShowSuccessFeedback");
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Console.WriteLine("✓ Show success feedback test passed\n");
        }

        #endregion

        #region Show Cooldown Tests

        private static void TestShowCooldown()
        {
            Console.WriteLine("Test 7: Show Cooldown");
            Console.WriteLine("---------------------");

            var thread = new Thread(() =>
            {
                var window = new OverlayWindow();

                // Initially hidden
                Assert(window.CooldownIndicator.Visibility == Visibility.Collapsed, 
                    "Cooldown indicator should be collapsed initially");
                Console.WriteLine("  ✓ Cooldown indicator initially collapsed");

                // Show cooldown with 2 seconds
                window.ShowCooldown(2);
                Assert(window.CooldownIndicator.Visibility == Visibility.Visible, 
                    "Cooldown indicator should be visible after ShowCooldown");
                Assert(window.CooldownText.Text == "2s", 
                    "Cooldown text should be '2s'");
                Console.WriteLine("  ✓ Cooldown indicator visible with '2s' text");

                // Show cooldown with different value
                window.ShowCooldown(5);
                Assert(window.CooldownText.Text == "5s", 
                    "Cooldown text should be '5s'");
                Console.WriteLine("  ✓ Cooldown text updated to '5s'");
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Console.WriteLine("✓ Show cooldown test passed\n");
        }

        #endregion

        #region Hide Cooldown Tests

        private static void TestHideCooldown()
        {
            Console.WriteLine("Test 8: Hide Cooldown");
            Console.WriteLine("---------------------");

            var thread = new Thread(() =>
            {
                var window = new OverlayWindow();

                // Show cooldown first
                window.ShowCooldown(2);
                Assert(window.CooldownIndicator.Visibility == Visibility.Visible, 
                    "Cooldown indicator should be visible after ShowCooldown");
                Console.WriteLine("  ✓ Cooldown indicator shown");

                // Hide cooldown
                window.HideCooldown();
                Assert(window.CooldownIndicator.Visibility == Visibility.Collapsed, 
                    "Cooldown indicator should be collapsed after HideCooldown");
                Console.WriteLine("  ✓ Cooldown indicator hidden");
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Console.WriteLine("✓ Hide cooldown test passed\n");
        }

        #endregion

        #region Custom Position Tests

        private static void TestCustomPosition()
        {
            Console.WriteLine("Test 9: Custom Position from Configuration");
            Console.WriteLine("-------------------------------------------");

            var thread = new Thread(() =>
            {
                // Test with custom position (50, 150)
                var customPosition = new Point(50, 150);
                var window = new OverlayWindow(customPosition);
                
                Assert(window.Left == 50, "Left position should be 50 from configuration");
                Assert(window.Top == 150, "Top position should be 150 from configuration");
                Console.WriteLine($"  ✓ Custom position loaded: ({window.Left}, {window.Top})");

                // Test with different custom position (200, 300)
                var anotherPosition = new Point(200, 300);
                var window2 = new OverlayWindow(anotherPosition);
                
                Assert(window2.Left == 200, "Left position should be 200 from configuration");
                Assert(window2.Top == 300, "Top position should be 300 from configuration");
                Console.WriteLine($"  ✓ Another custom position loaded: ({window2.Left}, {window2.Top})");
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Console.WriteLine("✓ Custom position test passed\n");
        }

        #endregion

        #region Position Changed Event Tests

        private static void TestPositionChangedEvent()
        {
            Console.WriteLine("Test 10: Position Changed Event");
            Console.WriteLine("--------------------------------");

            var thread = new Thread(() =>
            {
                var window = new OverlayWindow();
                bool eventRaised = false;
                Point? newPosition = null;

                // Subscribe to PositionChanged event
                window.PositionChanged += (sender, position) =>
                {
                    eventRaised = true;
                    newPosition = position;
                };

                // Update position should raise event
                window.UpdatePosition(123, 456);
                
                Assert(eventRaised, "PositionChanged event should be raised");
                Assert(newPosition.HasValue, "New position should be provided in event");
                if (newPosition.HasValue)
                {
                    Assert(newPosition.Value.X == 123, "Event should contain X = 123");
                    Assert(newPosition.Value.Y == 456, "Event should contain Y = 456");
                    Console.WriteLine($"  ✓ PositionChanged event raised with position ({newPosition.Value.X}, {newPosition.Value.Y})");
                }

                // Reset for second test
                eventRaised = false;
                newPosition = null;

                // Update to different position
                window.UpdatePosition(789, 101);
                
                Assert(eventRaised, "PositionChanged event should be raised again");
                Assert(newPosition.HasValue, "New position should be provided in event");
                if (newPosition.HasValue)
                {
                    Assert(newPosition.Value.X == 789, "Event should contain X = 789");
                    Assert(newPosition.Value.Y == 101, "Event should contain Y = 101");
                    Console.WriteLine($"  ✓ PositionChanged event raised again with position ({newPosition.Value.X}, {newPosition.Value.Y})");
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Console.WriteLine("✓ Position changed event test passed\n");
        }

        #endregion
    }
}
