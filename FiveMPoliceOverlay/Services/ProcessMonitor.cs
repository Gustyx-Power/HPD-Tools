using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace FiveMPoliceOverlay.Services
{
    /// <summary>
    /// Monitors for FiveM process start and stop events
    /// Polls every 2 seconds and fires events on state changes
    /// </summary>
    public class ProcessMonitor : IDisposable
    {
        private Timer? _pollingTimer;
        private bool _isFiveMRunning;
        private int _consecutiveFailures;
        private const int MaxConsecutiveFailures = 3;
        private const int PollingIntervalMs = 2000; // 2 seconds
        private bool _isDisposed;

        /// <summary>
        /// Fired when FiveM process is detected (transition from not running to running)
        /// </summary>
        public event EventHandler<ProcessEventArgs>? FiveMStarted;

        /// <summary>
        /// Fired when FiveM process terminates (transition from running to not running)
        /// </summary>
        public event EventHandler<ProcessEventArgs>? FiveMStopped;

        /// <summary>
        /// Starts monitoring for FiveM process
        /// </summary>
        public void StartMonitoring()
        {
            if (_pollingTimer != null)
            {
                return; // Already monitoring
            }

            _isFiveMRunning = false;
            _consecutiveFailures = 0;

            // Create timer that fires immediately and then every 2 seconds
            _pollingTimer = new Timer(
                CheckFiveMProcess,
                null,
                TimeSpan.Zero,
                TimeSpan.FromMilliseconds(PollingIntervalMs)
            );

            Console.WriteLine("[ProcessMonitor] Started monitoring for FiveM process");
        }

        /// <summary>
        /// Stops monitoring for FiveM process
        /// </summary>
        public void StopMonitoring()
        {
            if (_pollingTimer != null)
            {
                _pollingTimer.Dispose();
                _pollingTimer = null;
                Console.WriteLine("[ProcessMonitor] Stopped monitoring");
            }
        }

        /// <summary>
        /// Checks if FiveM is currently running
        /// </summary>
        /// <returns>True if FiveM process is detected, false otherwise</returns>
        public bool IsFiveMRunning()
        {
            try
            {
                // Check for FiveM.exe
                var fivemProcesses = Process.GetProcessesByName("FiveM");
                if (fivemProcesses.Length > 0)
                {
                    return true;
                }

                // Check for FiveM_GTAProcess.exe
                var gtaProcesses = Process.GetProcessesByName("FiveM_GTAProcess");
                if (gtaProcesses.Length > 0)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProcessMonitor] Error checking for FiveM process: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the FiveM process if it's running
        /// </summary>
        /// <returns>FiveM process or null if not found</returns>
        public Process? GetFiveMProcess()
        {
            try
            {
                // Prefer FiveM_GTAProcess.exe as it's the actual game process
                var gtaProcesses = Process.GetProcessesByName("FiveM_GTAProcess");
                if (gtaProcesses.Length > 0)
                {
                    return gtaProcesses.First();
                }

                // Fallback to FiveM.exe
                var fivemProcesses = Process.GetProcessesByName("FiveM");
                if (fivemProcesses.Length > 0)
                {
                    return fivemProcesses.First();
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProcessMonitor] Error getting FiveM process: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Timer callback that checks for FiveM process and fires events on state changes
        /// </summary>
        private void CheckFiveMProcess(object? state)
        {
            try
            {
                bool isRunning = IsFiveMRunning();

                // Reset consecutive failures on successful check
                if (isRunning || !isRunning) // Any successful check (whether found or not)
                {
                    _consecutiveFailures = 0;
                }

                // Detect state change: not running -> running
                if (isRunning && !_isFiveMRunning)
                {
                    _isFiveMRunning = true;
                    var process = GetFiveMProcess();
                    Console.WriteLine($"[ProcessMonitor] FiveM process detected (PID: {process?.Id ?? 0})");
                    OnFiveMStarted(new ProcessEventArgs(process));
                }
                // Detect state change: running -> not running
                else if (!isRunning && _isFiveMRunning)
                {
                    _isFiveMRunning = false;
                    Console.WriteLine("[ProcessMonitor] FiveM process terminated");
                    OnFiveMStopped(new ProcessEventArgs(null));
                }
            }
            catch (Exception ex)
            {
                _consecutiveFailures++;
                Console.WriteLine($"[ProcessMonitor] Error during process check (attempt {_consecutiveFailures}): {ex.Message}");

                // Log error after 3 consecutive failures
                if (_consecutiveFailures >= MaxConsecutiveFailures)
                {
                    Console.WriteLine($"[ProcessMonitor] ERROR: Failed to detect FiveM process after {MaxConsecutiveFailures} consecutive attempts");
                    Console.WriteLine($"[ProcessMonitor] Exception details: {ex}");
                    // Reset counter to avoid spamming logs
                    _consecutiveFailures = 0;
                }
            }
        }

        /// <summary>
        /// Raises the FiveMStarted event
        /// </summary>
        protected virtual void OnFiveMStarted(ProcessEventArgs e)
        {
            FiveMStarted?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the FiveMStopped event
        /// </summary>
        protected virtual void OnFiveMStopped(ProcessEventArgs e)
        {
            FiveMStopped?.Invoke(this, e);
        }

        /// <summary>
        /// Disposes the ProcessMonitor and stops monitoring
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                StopMonitoring();
                _isDisposed = true;
            }
        }
    }

    /// <summary>
    /// Event arguments for FiveM process events
    /// </summary>
    public class ProcessEventArgs : EventArgs
    {
        /// <summary>
        /// The FiveM process (null if process stopped)
        /// </summary>
        public Process? Process { get; }

        public ProcessEventArgs(Process? process)
        {
            Process = process;
        }
    }
}
