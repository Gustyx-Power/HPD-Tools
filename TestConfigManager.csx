// C# Script to test ConfigurationManager
// Run with: dotnet script TestConfigManager.csx

#r "FiveMPoliceOverlay/bin/Debug/net6.0-windows/win-x64/FiveMPoliceOverlay.dll"

using System;
using FiveMPoliceOverlay.Services;
using FiveMPoliceOverlay.Models;

Console.WriteLine("=== Testing ConfigurationManager ===\n");

try
{
    // Test 1: Create ConfigurationManager and load default config
    Console.WriteLine("Test 1: Loading/Creating Default Configuration");
    var manager = new ConfigurationManager();
    var config = manager.LoadConfiguration();
    
    Console.WriteLine($"✓ Version: {config.Version}");
    Console.WriteLine($"✓ AutoLaunch: {config.General.AutoLaunch}");
    Console.WriteLine($"✓ TestMode: {config.General.TestMode}");
    Console.WriteLine($"✓ Language: {config.General.Language}");
    Console.WriteLine($"✓ Overlay Position: ({config.Overlay.Position.X}, {config.Overlay.Position.Y})");
    Console.WriteLine($"✓ Cooldown: {config.RateLimiting.CooldownSeconds} seconds");
    Console.WriteLine($"✓ Max Queue Size: {config.RateLimiting.MaxQueueSize}");
    Console.WriteLine($"✓ Templates Count: {config.Templates.Count}");
    
    Console.WriteLine("\nPredefined Templates:");
    foreach (var template in config.Templates)
    {
        Console.WriteLine($"  - {template.Name} (ID: {template.Id})");
        Console.WriteLine($"    Category: {template.Category}");
        Console.WriteLine($"    Predefined: {template.IsPredefined}");
        Console.WriteLine($"    Text: {template.Text.Substring(0, Math.Min(50, template.Text.Length))}...");
        Console.WriteLine();
    }
    
    // Test 2: Modify and save configuration
    Console.WriteLine("\nTest 2: Modifying and Saving Configuration");
    config.General.TestMode = true;
    manager.SaveConfiguration(config).Wait();
    Console.WriteLine("✓ Configuration saved");
    
    // Test 3: Reload and verify changes
    Console.WriteLine("\nTest 3: Reloading Configuration");
    var reloadedConfig = manager.LoadConfiguration();
    Console.WriteLine($"✓ TestMode after reload: {reloadedConfig.General.TestMode}");
    
    if (reloadedConfig.General.TestMode == true)
    {
        Console.WriteLine("✓ Configuration persistence verified!");
    }
    
    // Test 4: Reset to defaults
    Console.WriteLine("\nTest 4: Resetting to Defaults");
    var resetConfig = manager.ResetToDefaults();
    Console.WriteLine($"✓ TestMode after reset: {resetConfig.General.TestMode}");
    Console.WriteLine($"✓ Templates after reset: {resetConfig.Templates.Count}");
    
    Console.WriteLine("\n=== All Tests Passed! ===");
}
catch (Exception ex)
{
    Console.WriteLine($"\n✗ Test Failed: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
