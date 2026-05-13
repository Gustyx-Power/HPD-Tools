using System;
using System.Threading;
using FiveMPoliceOverlay.Models;

namespace FiveMPoliceOverlay.Services
{
    /// <summary>
    /// Manual tests for RateLimiter service.
    /// Tests cooldown timing, queue management, and rate limit enforcement.
    /// Call RunTests() from your application to execute tests.
    /// </summary>
    public class RateLimiterTest
    {
        #region Test Helpers

        private static MessageTemplate CreateTemplate(string name, string text)
        {
            return new MessageTemplate
            {
                Id = Guid.NewGuid().ToString(),
                Category = "Test",
                Name = name,
                Text = text,
                IsPredefined = false
            };
        }

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
            Console.WriteLine("=== RateLimiter Test Suite ===\n");

            try
            {
                TestConstructors();
                TestInitialState();
                TestRateLimitCheck();
                TestCooldownTiming();
                TestEnqueueSuccess();
                TestEnqueueQueueFull();
                TestDequeueSuccess();
                TestDequeueEmpty();
                TestQueueManagement();
                TestClearQueue();
                TestReset();
                TestThreadSafety();

                Console.WriteLine("\n=== All Tests Completed Successfully ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[ERROR] Test failed: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        #endregion

        #region Constructor Tests

        private static void TestConstructors()
        {
            Console.WriteLine("Test 1: Constructors");
            Console.WriteLine("--------------------");

            // Test 1.1: Default constructor
            var limiter1 = new RateLimiter();
            Assert(limiter1.CooldownSeconds == 2, "Default cooldown should be 2 seconds");
            Assert(limiter1.MaxQueueSize == 5, "Default max queue size should be 5");

            // Test 1.2: Custom constructor
            var limiter2 = new RateLimiter(3, 10);
            Assert(limiter2.CooldownSeconds == 3, "Custom cooldown should be 3 seconds");
            Assert(limiter2.MaxQueueSize == 10, "Custom max queue size should be 10");

            // Test 1.3: Settings constructor
            var settings = new RateLimitSettings
            {
                CooldownSeconds = 4,
                MaxQueueSize = 8
            };
            var limiter3 = new RateLimiter(settings);
            Assert(limiter3.CooldownSeconds == 4, "Settings cooldown should be 4 seconds");
            Assert(limiter3.MaxQueueSize == 8, "Settings max queue size should be 8");

            // Test 1.4: Negative cooldown
            try
            {
                new RateLimiter(-1, 5);
                Assert(false, "Should throw ArgumentOutOfRangeException for negative cooldown");
            }
            catch (ArgumentOutOfRangeException)
            {
                // Expected
            }

            // Test 1.5: Negative max queue size
            try
            {
                new RateLimiter(2, -1);
                Assert(false, "Should throw ArgumentOutOfRangeException for negative max queue size");
            }
            catch (ArgumentOutOfRangeException)
            {
                // Expected
            }

            // Test 1.6: Null settings
            try
            {
                new RateLimiter(null!);
                Assert(false, "Should throw ArgumentNullException for null settings");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }

            Console.WriteLine("✓ Constructor tests passed\n");
        }

        #endregion

        #region Initial State Tests

        private static void TestInitialState()
        {
            Console.WriteLine("Test 2: Initial State");
            Console.WriteLine("---------------------");

            var limiter = new RateLimiter();

            // Test 2.1: Initial queue count
            Assert(limiter.QueueCount == 0, "Initial queue count should be 0");

            // Test 2.2: Initial rate limit state
            Assert(!limiter.IsRateLimited(), "Should not be rate limited initially");

            // Test 2.3: Initial cooldown remaining
            Assert(limiter.GetCooldownRemaining() == TimeSpan.Zero, "Initial cooldown remaining should be zero");

            // Test 2.4: Initial last message sent time
            Assert(limiter.LastMessageSentTime == DateTime.MinValue, "Initial last message sent time should be MinValue");

            Console.WriteLine("✓ Initial state tests passed\n");
        }

        #endregion

        #region Rate Limit Check Tests

        private static void TestRateLimitCheck()
        {
            Console.WriteLine("Test 3: Rate Limit Check");
            Console.WriteLine("------------------------");

            var limiter = new RateLimiter(1, 5); // 1 second cooldown for faster testing

            // Test 3.1: Not rate limited before any message sent
            Assert(!limiter.IsRateLimited(), "Should not be rate limited before any message sent");

            // Test 3.2: Rate limited after marking message sent
            limiter.MarkMessageSent();
            Assert(limiter.IsRateLimited(), "Should be rate limited after marking message sent");

            // Test 3.3: Not rate limited after cooldown expires
            Thread.Sleep(1100); // Wait for cooldown to expire
            Assert(!limiter.IsRateLimited(), "Should not be rate limited after cooldown expires");

            Console.WriteLine("✓ Rate limit check tests passed\n");
        }

        #endregion

        #region Cooldown Timing Tests

        private static void TestCooldownTiming()
        {
            Console.WriteLine("Test 4: Cooldown Timing");
            Console.WriteLine("-----------------------");

            var limiter = new RateLimiter(2, 5); // 2 second cooldown

            // Test 4.1: Cooldown remaining after message sent
            limiter.MarkMessageSent();
            var remaining1 = limiter.GetCooldownRemaining();
            Assert(remaining1 > TimeSpan.Zero, "Cooldown remaining should be greater than zero");
            Assert(remaining1 <= TimeSpan.FromSeconds(2), "Cooldown remaining should be <= 2 seconds");

            // Test 4.2: Cooldown decreases over time
            Thread.Sleep(500);
            var remaining2 = limiter.GetCooldownRemaining();
            Assert(remaining2 < remaining1, "Cooldown remaining should decrease over time");

            // Test 4.3: Cooldown reaches zero after expiry
            Thread.Sleep(1600); // Total wait: 2.1 seconds
            var remaining3 = limiter.GetCooldownRemaining();
            Assert(remaining3 == TimeSpan.Zero, "Cooldown remaining should be zero after expiry");

            Console.WriteLine("✓ Cooldown timing tests passed\n");
        }

        #endregion

        #region Enqueue Success Tests

        private static void TestEnqueueSuccess()
        {
            Console.WriteLine("Test 5: Enqueue Success");
            Console.WriteLine("-----------------------");

            var limiter = new RateLimiter(2, 5);

            // Test 5.1: Enqueue first message
            var template1 = CreateTemplate("Message 1", "First message");
            bool result1 = limiter.TryEnqueue(template1);
            Assert(result1 == true, "First enqueue should succeed");
            Assert(limiter.QueueCount == 1, "Queue count should be 1");

            // Test 5.2: Enqueue multiple messages
            var template2 = CreateTemplate("Message 2", "Second message");
            var template3 = CreateTemplate("Message 3", "Third message");
            limiter.TryEnqueue(template2);
            limiter.TryEnqueue(template3);
            Assert(limiter.QueueCount == 3, "Queue count should be 3");

            // Test 5.3: Null template
            try
            {
                limiter.TryEnqueue(null!);
                Assert(false, "Should throw ArgumentNullException for null template");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }

            Console.WriteLine("✓ Enqueue success tests passed\n");
        }

        #endregion

        #region Enqueue Queue Full Tests

        private static void TestEnqueueQueueFull()
        {
            Console.WriteLine("Test 6: Enqueue Queue Full");
            Console.WriteLine("---------------------------");

            var limiter = new RateLimiter(2, 3); // Max 3 messages

            // Test 6.1: Fill queue to capacity
            var template1 = CreateTemplate("Message 1", "First");
            var template2 = CreateTemplate("Message 2", "Second");
            var template3 = CreateTemplate("Message 3", "Third");
            limiter.TryEnqueue(template1);
            limiter.TryEnqueue(template2);
            limiter.TryEnqueue(template3);
            Assert(limiter.QueueCount == 3, "Queue should be full");

            // Test 6.2: Enqueue when queue is full
            var template4 = CreateTemplate("Message 4", "Fourth");
            bool result = limiter.TryEnqueue(template4);
            Assert(result == false, "Enqueue should fail when queue is full");
            Assert(limiter.QueueCount == 3, "Queue count should remain 3");

            Console.WriteLine("✓ Enqueue queue full tests passed\n");
        }

        #endregion

        #region Dequeue Success Tests

        private static void TestDequeueSuccess()
        {
            Console.WriteLine("Test 7: Dequeue Success");
            Console.WriteLine("-----------------------");

            var limiter = new RateLimiter(2, 5);

            // Test 7.1: Enqueue and dequeue
            var template1 = CreateTemplate("Message 1", "First message");
            limiter.TryEnqueue(template1);
            bool result = limiter.TryDequeue(out var dequeuedTemplate);
            Assert(result == true, "Dequeue should succeed");
            Assert(dequeuedTemplate != null, "Dequeued template should not be null");
            if (dequeuedTemplate != null)
            {
                Assert(dequeuedTemplate.Name == "Message 1", "Dequeued template should match");
            }
            Assert(limiter.QueueCount == 0, "Queue should be empty after dequeue");

            // Test 7.2: FIFO order
            var template2 = CreateTemplate("Message 2", "Second");
            var template3 = CreateTemplate("Message 3", "Third");
            limiter.TryEnqueue(template2);
            limiter.TryEnqueue(template3);
            limiter.TryDequeue(out var dequeued1);
            limiter.TryDequeue(out var dequeued2);
            if (dequeued1 != null && dequeued2 != null)
            {
                Assert(dequeued1.Name == "Message 2", "First dequeued should be Message 2");
                Assert(dequeued2.Name == "Message 3", "Second dequeued should be Message 3");
            }

            Console.WriteLine("✓ Dequeue success tests passed\n");
        }

        #endregion

        #region Dequeue Empty Tests

        private static void TestDequeueEmpty()
        {
            Console.WriteLine("Test 8: Dequeue Empty");
            Console.WriteLine("---------------------");

            var limiter = new RateLimiter(2, 5);

            // Test 8.1: Dequeue from empty queue
            bool result = limiter.TryDequeue(out var template);
            Assert(result == false, "Dequeue should fail on empty queue");
            Assert(template == null, "Template should be null when dequeue fails");

            // Test 8.2: Dequeue after queue becomes empty
            var template1 = CreateTemplate("Message 1", "First");
            limiter.TryEnqueue(template1);
            limiter.TryDequeue(out _);
            bool result2 = limiter.TryDequeue(out var template2);
            Assert(result2 == false, "Dequeue should fail after queue becomes empty");
            Assert(template2 == null, "Template should be null");

            Console.WriteLine("✓ Dequeue empty tests passed\n");
        }

        #endregion

        #region Queue Management Tests

        private static void TestQueueManagement()
        {
            Console.WriteLine("Test 9: Queue Management");
            Console.WriteLine("------------------------");

            var limiter = new RateLimiter(2, 5);

            // Test 9.1: Queue count updates correctly
            Assert(limiter.QueueCount == 0, "Initial queue count should be 0");
            limiter.TryEnqueue(CreateTemplate("M1", "First"));
            Assert(limiter.QueueCount == 1, "Queue count should be 1 after enqueue");
            limiter.TryEnqueue(CreateTemplate("M2", "Second"));
            Assert(limiter.QueueCount == 2, "Queue count should be 2 after second enqueue");
            limiter.TryDequeue(out _);
            Assert(limiter.QueueCount == 1, "Queue count should be 1 after dequeue");
            limiter.TryDequeue(out _);
            Assert(limiter.QueueCount == 0, "Queue count should be 0 after all dequeued");

            Console.WriteLine("✓ Queue management tests passed\n");
        }

        #endregion

        #region Clear Queue Tests

        private static void TestClearQueue()
        {
            Console.WriteLine("Test 10: Clear Queue");
            Console.WriteLine("--------------------");

            var limiter = new RateLimiter(2, 5);

            // Test 10.1: Clear non-empty queue
            limiter.TryEnqueue(CreateTemplate("M1", "First"));
            limiter.TryEnqueue(CreateTemplate("M2", "Second"));
            limiter.TryEnqueue(CreateTemplate("M3", "Third"));
            Assert(limiter.QueueCount == 3, "Queue should have 3 messages");
            limiter.ClearQueue();
            Assert(limiter.QueueCount == 0, "Queue should be empty after clear");

            // Test 10.2: Clear empty queue
            limiter.ClearQueue();
            Assert(limiter.QueueCount == 0, "Clearing empty queue should not cause issues");

            // Test 10.3: Enqueue after clear
            limiter.TryEnqueue(CreateTemplate("M4", "Fourth"));
            Assert(limiter.QueueCount == 1, "Should be able to enqueue after clear");

            Console.WriteLine("✓ Clear queue tests passed\n");
        }

        #endregion

        #region Reset Tests

        private static void TestReset()
        {
            Console.WriteLine("Test 11: Reset");
            Console.WriteLine("--------------");

            var limiter = new RateLimiter(1, 5); // 1 second cooldown

            // Test 11.1: Reset clears queue
            limiter.TryEnqueue(CreateTemplate("M1", "First"));
            limiter.TryEnqueue(CreateTemplate("M2", "Second"));
            limiter.Reset();
            Assert(limiter.QueueCount == 0, "Queue should be empty after reset");

            // Test 11.2: Reset clears cooldown
            limiter.MarkMessageSent();
            Assert(limiter.IsRateLimited(), "Should be rate limited before reset");
            limiter.Reset();
            Assert(!limiter.IsRateLimited(), "Should not be rate limited after reset");
            Assert(limiter.GetCooldownRemaining() == TimeSpan.Zero, "Cooldown should be zero after reset");

            // Test 11.3: Reset resets last message sent time
            limiter.MarkMessageSent();
            limiter.Reset();
            Assert(limiter.LastMessageSentTime == DateTime.MinValue, "Last message sent time should be MinValue after reset");

            Console.WriteLine("✓ Reset tests passed\n");
        }

        #endregion

        #region Thread Safety Tests

        private static void TestThreadSafety()
        {
            Console.WriteLine("Test 12: Thread Safety");
            Console.WriteLine("----------------------");

            var limiter = new RateLimiter(2, 100);
            int successfulEnqueues = 0;
            int failedEnqueues = 0;
            object countLock = new object();

            // Test 12.1: Concurrent enqueues
            var threads = new Thread[10];
            for (int i = 0; i < threads.Length; i++)
            {
                int threadId = i;
                threads[i] = new Thread(() =>
                {
                    for (int j = 0; j < 10; j++)
                    {
                        var template = CreateTemplate($"T{threadId}-M{j}", $"Thread {threadId} Message {j}");
                        bool result = limiter.TryEnqueue(template);
                        lock (countLock)
                        {
                            if (result)
                                successfulEnqueues++;
                            else
                                failedEnqueues++;
                        }
                    }
                });
                threads[i].Start();
            }

            // Wait for all threads to complete
            foreach (var thread in threads)
            {
                thread.Join();
            }

            Console.WriteLine($"  Successful enqueues: {successfulEnqueues}");
            Console.WriteLine($"  Failed enqueues: {failedEnqueues}");
            Console.WriteLine($"  Final queue count: {limiter.QueueCount}");

            Assert(limiter.QueueCount == successfulEnqueues, "Queue count should match successful enqueues");
            Assert(successfulEnqueues + failedEnqueues == 100, "Total operations should be 100");

            Console.WriteLine("✓ Thread safety tests passed\n");
        }

        #endregion
    }
}
