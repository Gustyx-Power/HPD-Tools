# PowerShell script to test ConfigurationManager functionality
Write-Host "=== Testing ConfigurationManager ===" -ForegroundColor Cyan

# Clean up any existing config
$configPath = "$env:APPDATA\FiveMPoliceOverlay\config.json"
$backupPath = "$configPath.bak"

Write-Host "`nCleaning up existing config files..." -ForegroundColor Yellow
if (Test-Path $configPath) {
    Remove-Item $configPath -Force
    Write-Host "Removed existing config.json" -ForegroundColor Green
}
if (Test-Path $backupPath) {
    Remove-Item $backupPath -Force
    Write-Host "Removed existing config.json.bak" -ForegroundColor Green
}

# Build the project
Write-Host "`nBuilding project..." -ForegroundColor Yellow
dotnet build FiveMPoliceOverlay/FiveMPoliceOverlay.csproj --configuration Debug
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "Build succeeded!" -ForegroundColor Green

# Test 1: Check if default config is created
Write-Host "`n=== Test 1: Default Configuration Creation ===" -ForegroundColor Cyan
Write-Host "Expected: config.json should be created with 4 predefined templates"

# We'll need to manually verify by checking the file
if (Test-Path $configPath) {
    Write-Host "Config file created at: $configPath" -ForegroundColor Green
    
    $config = Get-Content $configPath | ConvertFrom-Json
    Write-Host "Version: $($config.version)" -ForegroundColor Green
    Write-Host "Templates count: $($config.templates.Count)" -ForegroundColor Green
    
    if ($config.templates.Count -eq 4) {
        Write-Host "All 4 predefined templates exist" -ForegroundColor Green
        foreach ($template in $config.templates) {
            Write-Host "  - $($template.name) (ID: $($template.id))" -ForegroundColor Gray
        }
    }
    else {
        Write-Host "Expected 4 templates, found $($config.templates.Count)" -ForegroundColor Red
    }
}
else {
    Write-Host "Config file not created" -ForegroundColor Red
}

# Test 2: Test corrupted file handling
Write-Host "`n=== Test 2: Corrupted File Handling ===" -ForegroundColor Cyan
Write-Host "Creating corrupted config file..."
"{ invalid json !!!" | Out-File -FilePath $configPath -Encoding UTF8
Write-Host "Corrupted file created" -ForegroundColor Yellow

Write-Host "`nNote: To fully test corrupted file handling, you would need to:"
Write-Host "1. Run the application or call LoadConfiguration()"
Write-Host "2. Verify that config.json.bak is created"
Write-Host "3. Verify that a new valid config.json is created"

# Test 3: Verify JSON structure
Write-Host "`n=== Test 3: JSON Structure Verification ===" -ForegroundColor Cyan
if (Test-Path $configPath) {
    Remove-Item $configPath -Force
}

# The config will be created on first load
Write-Host "Config structure should include:"
Write-Host "  - version: '1.0'" -ForegroundColor Gray
Write-Host "  - general: { autoLaunch, testMode, language }" -ForegroundColor Gray
Write-Host "  - overlay: { position, isVisible, toggleKeybind }" -ForegroundColor Gray
Write-Host "  - keybinds: []" -ForegroundColor Gray
Write-Host "  - templates: [4 predefined templates]" -ForegroundColor Gray
Write-Host "  - rateLimiting: { cooldownSeconds, maxQueueSize }" -ForegroundColor Gray

Write-Host "`n=== Manual Testing Instructions ===" -ForegroundColor Cyan
Write-Host "To fully test ConfigurationManager, you can:"
Write-Host "1. Add a call to ConfigurationManagerTest.RunTests() in your App.xaml.cs"
Write-Host "2. Run the application and check the console output"
Write-Host "3. Verify the config file at: $configPath"

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan
