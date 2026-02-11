# FlipKit Release Build Script
# Builds Desktop, Web, and API packages for all platforms

param(
    [string]$Version = "3.0.0"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "FlipKit v$Version Release Build" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Clean old releases
Write-Host "Cleaning old release folder..." -ForegroundColor Yellow
if (Test-Path ".\releases") {
    Remove-Item ".\releases" -Recurse -Force
}
New-Item -ItemType Directory -Path ".\releases" | Out-Null

# ============================================================================
# DESKTOP APP BUILDS
# ============================================================================

Write-Host ""
Write-Host "Building Desktop App..." -ForegroundColor Green

$desktopTargets = @(
    @{ Runtime = "win-x64"; Name = "Windows-x64" },
    @{ Runtime = "osx-x64"; Name = "macOS-Intel" },
    @{ Runtime = "osx-arm64"; Name = "macOS-ARM" }
)

foreach ($target in $desktopTargets) {
    Write-Host "  - Building $($target.Name)..." -ForegroundColor Yellow

    dotnet publish FlipKit.Desktop `
        -c Release `
        -r $target.Runtime `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o ".\releases\temp\desktop-$($target.Runtime)"

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed for $($target.Name)"
    }

    # Create zip
    $zipName = "FlipKit-Desktop-$($target.Name)-v$Version.zip"
    Compress-Archive `
        -Path ".\releases\temp\desktop-$($target.Runtime)\*" `
        -DestinationPath ".\releases\$zipName" `
        -Force

    Write-Host "    Created: $zipName" -ForegroundColor Green
}

# ============================================================================
# WEB APP BUILDS
# ============================================================================

Write-Host ""
Write-Host "Building Web App..." -ForegroundColor Green

$webTargets = @(
    @{ Runtime = "win-x64"; Name = "Windows-x64"; Launcher = "StartWeb.bat" },
    @{ Runtime = "osx-x64"; Name = "macOS-Intel"; Launcher = "start-web.sh" },
    @{ Runtime = "osx-arm64"; Name = "macOS-ARM"; Launcher = "start-web.sh" },
    @{ Runtime = "linux-x64"; Name = "Linux-x64"; Launcher = "start-web.sh" }
)

foreach ($target in $webTargets) {
    Write-Host "  - Building $($target.Name)..." -ForegroundColor Yellow

    dotnet publish FlipKit.Web `
        -c Release `
        -r $target.Runtime `
        --self-contained true `
        -o ".\releases\temp\web-$($target.Runtime)"

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed for Web $($target.Name)"
    }

    # Create launcher script
    $launcherPath = ".\releases\temp\web-$($target.Runtime)\$($target.Launcher)"

    if ($target.Runtime -like "win-*") {
        @"
@echo off
echo Starting FlipKit Web App...
echo.
echo Web app will open in your browser at http://localhost:5001
echo.
echo To access from your phone:
echo 1. Make sure phone and computer are on same WiFi or Tailscale network
echo 2. Find your computer's IP address
echo 3. Open browser on phone and go to http://YOUR-IP:5001
echo.
start http://localhost:5001
FlipKit.Web.exe --urls "http://0.0.0.0:5001"
"@ | Out-File -FilePath $launcherPath -Encoding ASCII
    } else {
        @"
#!/bin/bash
echo "Starting FlipKit Web App..."
echo ""
echo "Web app will open in your browser at http://localhost:5001"
echo ""
echo "To access from your phone:"
echo "1. Make sure phone and computer are on same WiFi or Tailscale network"
echo "2. Find your computer's IP address"
echo "3. Open browser on phone and go to http://YOUR-IP:5001"
echo ""

# Try to open browser
if command -v open &> /dev/null; then
    open http://localhost:5001
elif command -v xdg-open &> /dev/null; then
    xdg-open http://localhost:5001
fi

# Run web app
./FlipKit.Web --urls "http://0.0.0.0:5001"
"@ | Out-File -FilePath $launcherPath -Encoding UTF8
        # Make executable
        if ($IsLinux -or $IsMacOS) {
            chmod +x $launcherPath
        }
    }

    # Create archive
    $ext = if ($target.Runtime -like "linux-*") { "tar.gz" } else { "zip" }
    $archiveName = "FlipKit-Web-$($target.Name)-v$Version.$ext"

    if ($ext -eq "zip") {
        Compress-Archive `
            -Path ".\releases\temp\web-$($target.Runtime)\*" `
            -DestinationPath ".\releases\$archiveName" `
            -Force
    } else {
        tar -czf ".\releases\$archiveName" -C ".\releases\temp\web-$($target.Runtime)" .
    }

    Write-Host "    Created: $archiveName" -ForegroundColor Green
}

# ============================================================================
# API SERVER BUILDS
# ============================================================================

Write-Host ""
Write-Host "Building API Server..." -ForegroundColor Green

$apiTargets = @(
    @{ Runtime = "win-x64"; Name = "Windows-x64"; Launcher = "StartAPI.bat" },
    @{ Runtime = "osx-x64"; Name = "macOS-Intel"; Launcher = "start-api.sh" },
    @{ Runtime = "osx-arm64"; Name = "macOS-ARM"; Launcher = "start-api.sh" },
    @{ Runtime = "linux-x64"; Name = "Linux-x64"; Launcher = "start-api.sh" }
)

foreach ($target in $apiTargets) {
    Write-Host "  - Building $($target.Name)..." -ForegroundColor Yellow

    dotnet publish FlipKit.Api `
        -c Release `
        -r $target.Runtime `
        --self-contained true `
        -o ".\releases\temp\api-$($target.Runtime)"

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed for API $($target.Name)"
    }

    # Create launcher script
    $launcherPath = ".\releases\temp\api-$($target.Runtime)\$($target.Launcher)"

    if ($target.Runtime -like "win-*") {
        @"
@echo off
echo Starting FlipKit API Server...
echo.
echo API will be available at:
echo - http://localhost:5000 (local access)
echo - http://YOUR-TAILSCALE-IP:5000 (remote access)
echo.
echo Get your Tailscale IP: tailscale ip -4
echo.
FlipKit.Api.exe
"@ | Out-File -FilePath $launcherPath -Encoding ASCII
    } else {
        @"
#!/bin/bash
echo "Starting FlipKit API Server..."
echo ""
echo "API will be available at:"
echo "- http://localhost:5000 (local access)"
echo "- http://YOUR-TAILSCALE-IP:5000 (remote access)"
echo ""
echo "Get your Tailscale IP: tailscale ip -4"
echo ""

./FlipKit.Api
"@ | Out-File -FilePath $launcherPath -Encoding UTF8
        # Make executable
        if ($IsLinux -or $IsMacOS) {
            chmod +x $launcherPath
        }
    }

    # Create archive
    $ext = if ($target.Runtime -like "linux-*") { "tar.gz" } else { "zip" }
    $archiveName = "FlipKit-API-$($target.Name)-v$Version.$ext"

    if ($ext -eq "zip") {
        Compress-Archive `
            -Path ".\releases\temp\api-$($target.Runtime)\*" `
            -DestinationPath ".\releases\$archiveName" `
            -Force
    } else {
        tar -czf ".\releases\$archiveName" -C ".\releases\temp\api-$($target.Runtime)" .
    }

    Write-Host "    Created: $archiveName" -ForegroundColor Green
}

# ============================================================================
# CLEANUP
# ============================================================================

Write-Host ""
Write-Host "Cleaning up temporary files..." -ForegroundColor Yellow
Remove-Item ".\releases\temp" -Recurse -Force

# ============================================================================
# SUMMARY
# ============================================================================

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Release packages created in: .\releases\" -ForegroundColor White
Write-Host ""
Get-ChildItem ".\releases\*.zip", ".\releases\*.tar.gz" | ForEach-Object {
    $sizeMB = [math]::Round($_.Length / 1MB, 2)
    Write-Host "  $($_.Name) - $sizeMB MB" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Test the builds" -ForegroundColor White
Write-Host "2. Create git tag: git tag v$Version" -ForegroundColor White
Write-Host "3. Push tag: git push origin v$Version" -ForegroundColor White
Write-Host "4. Create GitHub release and upload packages" -ForegroundColor White
Write-Host ""
