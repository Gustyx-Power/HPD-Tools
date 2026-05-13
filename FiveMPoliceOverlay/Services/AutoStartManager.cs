using System;
using Microsoft.Win32;

namespace FiveMPoliceOverlay.Services
{
    /// <summary>
    /// Manages Windows auto-start functionality via registry.
    /// Writes to HKCU\Software\Microsoft\Windows\CurrentVersion\Run
    /// </summary>
    public static class AutoStartManager
    {
        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppRegistryName = "FiveMPoliceOverlay";

        /// <summary>
        /// Enables auto-start on Windows login by writing executable path to registry.
        /// </summary>
        /// <returns>True if registry write succeeded</returns>
        public static bool EnableAutoStart()
        {
            try
            {
                string exePath = GetExecutablePath();
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);

                if (key == null)
                {
                    Console.WriteLine("[AutoStartManager] ERROR: Cannot open registry key");
                    return false;
                }

                key.SetValue(AppRegistryName, $"\"{exePath}\"");
                Console.WriteLine($"[AutoStartManager] Auto-start enabled: {exePath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AutoStartManager] ERROR enabling auto-start: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disables auto-start by removing registry entry.
        /// </summary>
        /// <returns>True if registry deletion succeeded or key didn't exist</returns>
        public static bool DisableAutoStart()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);

                if (key == null)
                {
                    Console.WriteLine("[AutoStartManager] Registry key not found, nothing to remove");
                    return true;
                }

                if (key.GetValue(AppRegistryName) != null)
                {
                    key.DeleteValue(AppRegistryName, throwOnMissingValue: false);
                    Console.WriteLine("[AutoStartManager] Auto-start disabled");
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AutoStartManager] ERROR disabling auto-start: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if auto-start is currently enabled.
        /// </summary>
        /// <returns>True if registry entry exists</returns>
        public static bool IsAutoStartEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: false);
                return key?.GetValue(AppRegistryName) != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AutoStartManager] ERROR checking auto-start: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the full path to the current executable.
        /// </summary>
        private static string GetExecutablePath()
        {
            return System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                   ?? AppDomain.CurrentDomain.BaseDirectory + "FiveMPoliceOverlay.exe";
        }
    }
}
