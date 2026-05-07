# Test script for ProcessMonitor service
# This script demonstrates how to test the ProcessMonitor functionality

Write-Host "=== ProcessMonitor Test Script ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "This script will help you test the ProcessMonitor service." -ForegroundColor Yellow
Write-Host ""

Write-Host "Test Instructions:" -ForegroundColor Green
Write-Host "1. The ProcessMonitor polls every 2 seconds for FiveM processes"
Write-Host "2. It checks for 'FiveM.exe' and 'FiveM_GTAProcess.exe'"
Write-Host "3. Events fire when FiveM starts (not running -> running)"
Write-Host "4. Events fire when FiveM stops (running -> not running)"
Write-Host "5. After 3 consecutive failed detection attempts, errors are logged"
Write-Host ""

Write-Host "To test manually:" -ForegroundColor Cyan
Write-Host "1. Build the project: dotnet build FiveMPoliceOverlay/FiveMPoliceOverlay.csproj"
Write-Host "2. Add ProcessMonitorTest.RunTests() to your App.xaml.cs"
Write-Host "3. Run the application and observe console output"
Write-Host "4. Start/stop FiveM to trigger events"
Write-Host ""

Write-Host "Checking current FiveM process status..." -ForegroundColor Yellow
$fivemProcess = Get-Process -Name "FiveM", "FiveM_GTAProcess" -ErrorAction SilentlyContinue

if ($fivemProcess) {
    Write-Host "✓ FiveM is currently running:" -ForegroundColor Green
    $fivemProcess | ForEach-Object {
        Write-Host "  - $($_.ProcessName) (PID: $($_.Id))" -ForegroundColor White
    }
    Write-Host ""
    Write-Host "To test FiveMStopped event: Close FiveM and observe the event firing" -ForegroundColor Cyan
} else {
    Write-Host "✗ FiveM is not currently running" -ForegroundColor Red
    Write-Host ""
    Write-Host "To test FiveMStarted event: Start FiveM and observe the event firing" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "=== Test Complete ===" -ForegroundColor Cyan
