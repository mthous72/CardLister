# FlipKit Hub Release Build Script
# Builds unified Hub packages for Windows and Linux
# Version: 3.1.0

param(
    [string]$Version = "3.1.0"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "FlipKit Hub v$Version Release Build" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Building unified packages for Windows and Linux" -ForegroundColor Yellow
Write-Host "(macOS excluded due to code signing requirements)" -ForegroundColor DarkGray
Write-Host ""

# Clean old releases
Write-Host "Cleaning old release folder..." -ForegroundColor Yellow
if (Test-Path ".\releases") {
    Remove-Item ".\releases" -Recurse -Force
}
New-Item -ItemType Directory -Path ".\releases" | Out-Null
New-Item -ItemType Directory -Path ".\releases\temp" | Out-Null

# ============================================================================
# FLIPKIT HUB UNIFIED PACKAGES
# ============================================================================

$hubTargets = @(
    @{ Runtime = "win-x64"; Name = "Windows-x64"; Ext = "zip" },
    @{ Runtime = "linux-x64"; Name = "Linux-x64"; Ext = "tar.gz" }
)

foreach ($target in $hubTargets) {
    Write-Host ""
    Write-Host "Building FlipKit Hub for $($target.Name)..." -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Green

    $hubDir = ".\releases\temp\FlipKit-Hub-$($target.Name)-v$Version"
    $serversDir = "$hubDir\servers"

    # Create folder structure
    New-Item -ItemType Directory -Path $hubDir -Force | Out-Null
    New-Item -ItemType Directory -Path $serversDir -Force | Out-Null

    # -------------------------------------------------------------------------
    # 1. Build Desktop App (root folder)
    # -------------------------------------------------------------------------
    Write-Host "  [1/3] Building Desktop app..." -ForegroundColor Yellow

    dotnet publish FlipKit.Desktop `
        -c Release `
        -r $target.Runtime `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o "$hubDir"

    if ($LASTEXITCODE -ne 0) {
        throw "Desktop build failed for $($target.Name)"
    }

    Write-Host "    Desktop app built successfully" -ForegroundColor Green

    # -------------------------------------------------------------------------
    # 2. Build Web Server (servers/ folder)
    # -------------------------------------------------------------------------
    Write-Host "  [2/3] Building Web server..." -ForegroundColor Yellow

    $webTempDir = ".\releases\temp\web-$($target.Runtime)"
    dotnet publish FlipKit.Web `
        -c Release `
        -r $target.Runtime `
        --self-contained true `
        -o "$webTempDir"

    if ($LASTEXITCODE -ne 0) {
        throw "Web build failed for $($target.Name)"
    }

    # Move Web server to servers/ folder
    Move-Item "$webTempDir\*" "$serversDir\" -Force
    Remove-Item $webTempDir -Recurse -Force

    Write-Host "    Web server built successfully" -ForegroundColor Green

    # -------------------------------------------------------------------------
    # 3. Build API Server (servers/ folder)
    # -------------------------------------------------------------------------
    Write-Host "  [3/3] Building API server..." -ForegroundColor Yellow

    $apiTempDir = ".\releases\temp\api-$($target.Runtime)"
    dotnet publish FlipKit.Api `
        -c Release `
        -r $target.Runtime `
        --self-contained true `
        -o "$apiTempDir"

    if ($LASTEXITCODE -ne 0) {
        throw "API build failed for $($target.Name)"
    }

    # Move API server to servers/ folder
    Move-Item "$apiTempDir\*" "$serversDir\" -Force
    Remove-Item $apiTempDir -Recurse -Force

    Write-Host "    API server built successfully" -ForegroundColor Green

    # -------------------------------------------------------------------------
    # 4. Copy Documentation
    # -------------------------------------------------------------------------
    Write-Host "  [4/4] Copying documentation..." -ForegroundColor Yellow

    $docsDir = "$hubDir\Docs"
    New-Item -ItemType Directory -Path $docsDir -Force | Out-Null

    # Copy key documentation files
    Copy-Item ".\Docs\USER-GUIDE.md" "$docsDir\" -ErrorAction SilentlyContinue
    Copy-Item ".\Docs\WEB-USER-GUIDE.md" "$docsDir\" -ErrorAction SilentlyContinue
    Copy-Item ".\Docs\DEPLOYMENT-GUIDE.md" "$docsDir\" -ErrorAction SilentlyContinue
    Copy-Item ".\README.md" "$docsDir\" -ErrorAction SilentlyContinue
    Copy-Item ".\LICENSE" "$hubDir\" -ErrorAction SilentlyContinue

    # -------------------------------------------------------------------------
    # 5. Create README.txt with Quick Start
    # -------------------------------------------------------------------------

    $readmeContent = @"
========================================
FlipKit Hub v$Version
========================================

A unified package containing FlipKit Desktop, Web, and API servers.

QUICK START
-----------

1. Launch FlipKit.Desktop$( if ($target.Runtime -like "win-*") { ".exe" } else { "" } )
2. Servers auto-start automatically (configurable in Settings)
3. Access Web UI from your phone:
   - Connect phone to same Wi-Fi network
   - Scan QR code in Settings, or
   - Navigate to http://YOUR-IP:5000

WHAT'S INCLUDED
---------------

- FlipKit Desktop (main application)
- Web Server (mobile access on port 5000)
- API Server (remote access on port 5001)

All servers are managed from Desktop app Settings.

DOCUMENTATION
-------------

See the Docs/ folder for complete guides:
- USER-GUIDE.md - Desktop app features
- WEB-USER-GUIDE.md - Mobile web interface
- DEPLOYMENT-GUIDE.md - Advanced setup
- README.md - Project overview

SYSTEM REQUIREMENTS
-------------------

- $($target.Name)
- Network connection (for mobile access)
- No additional software required (self-contained)

FEATURES
--------

- AI-powered card scanning (11 free vision models)
- Inventory management with filtering and search
- Pricing research via eBay/Terapeak
- Whatnot CSV export with ImgBB hosting
- Sales tracking and financial reports
- Mobile web interface for on-the-go scanning
- Shared SQLite database (WAL mode)

GETTING HELP
------------

- Documentation: See Docs/ folder
- Issues: https://github.com/mthous72/FlipKit/issues

========================================
"@

    $readmeContent | Out-File -FilePath "$hubDir\README.txt" -Encoding UTF8

    Write-Host "    Documentation added" -ForegroundColor Green

    # -------------------------------------------------------------------------
    # 6. Create Archive
    # -------------------------------------------------------------------------
    Write-Host "  Creating archive..." -ForegroundColor Yellow

    $archiveName = "FlipKit-Hub-$($target.Name)-v$Version.$($target.Ext)"

    if ($target.Ext -eq "zip") {
        # Windows zip
        Compress-Archive `
            -Path "$hubDir\*" `
            -DestinationPath ".\releases\$archiveName" `
            -Force
    } else {
        # Linux tar.gz
        $currentDir = Get-Location
        Set-Location $hubDir
        tar -czf "$currentDir\releases\$archiveName" *
        Set-Location $currentDir
    }

    Write-Host ""
    Write-Host "  ✓ Created: $archiveName" -ForegroundColor Green

    # Calculate size
    $size = (Get-Item ".\releases\$archiveName").Length / 1MB
    Write-Host "    Size: $([math]::Round($size, 2)) MB" -ForegroundColor Cyan

    # Clean up temp folder for this target
    Remove-Item $hubDir -Recurse -Force
}

# ============================================================================
# BUILD COMPLETE
# ============================================================================

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# List all packages
$packages = Get-ChildItem ".\releases\*.zip", ".\releases\*.tar.gz" | Select-Object Name, @{Name="Size (MB)";Expression={[math]::Round($_.Length / 1MB, 2)}}

Write-Host "Packages created:" -ForegroundColor Yellow
$packages | Format-Table -AutoSize

$totalSize = ($packages | Measure-Object -Property "Size (MB)" -Sum).Sum
Write-Host "Total size: $([math]::Round($totalSize, 2)) MB" -ForegroundColor Cyan

Write-Host ""
Write-Host "Release files are in: .\releases\" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Test packages on Windows and Linux" -ForegroundColor White
Write-Host "2. Create GitHub release v$Version" -ForegroundColor White
Write-Host "3. Upload packages to release" -ForegroundColor White
Write-Host "4. Update README.md with new download links" -ForegroundColor White
Write-Host ""
