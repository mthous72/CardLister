# FlipKit Windows Installer

This directory contains the Inno Setup script for creating a unified Windows installer for FlipKit.

## Prerequisites

1. **Inno Setup 6.x** - Download from https://jrsoftware.org/isdl.php
2. **Built packages** - Run `build-release.ps1` first to generate platform packages

## Building the Installer

### Option 1: Using Inno Setup Compiler (Command Line)

```powershell
# Make sure you've built the packages first
.\build-release.ps1 -Version "3.0.0"

# Compile the installer
iscc installer\flipkit-setup.iss
```

### Option 2: Using Inno Setup GUI

1. Open **Inno Setup Compiler**
2. File → Open → Select `installer\flipkit-setup.iss`
3. Build → Compile
4. Output will be in `releases\installer\FlipKit-Setup-v3.0.0.exe`

## Installer Features

### Component Options

Users can choose which components to install:

1. **Desktop Application** (Required)
   - Full-featured Windows app
   - Always installed (fixed component)

2. **Web Server** (Optional)
   - ASP.NET Core web app for mobile browser access
   - Accessible on local network at http://localhost:5001

3. **API Server** (Optional)
   - REST API for remote access via Tailscale
   - Used for Desktop/Web apps on other devices

4. **Documentation** (Optional)
   - User guides and deployment documentation

### Installation Types

- **Full Installation** - All components (Desktop + Web + API + Docs)
- **Desktop Only** - Just the desktop app
- **Web Server Only** - Just the web server for mobile use
- **Custom** - Pick and choose components

### Shortcuts Created

**Desktop app:**
- Start Menu: `FlipKit`
- Desktop: `FlipKit` (optional)

**Web server:**
- Start Menu: `FlipKit Web Server` (starts server)
- Start Menu: `Open FlipKit Web (Browser)` (opens browser)

**API server:**
- Start Menu: `FlipKit API Server`

**Documentation:**
- Start Menu: `FlipKit User Guide`
- Start Menu: `FlipKit Web Guide`

### CardLister Migration

The installer detects existing CardLister installations and notifies users that their data will be automatically migrated on first launch. The original CardLister data is preserved as a backup.

## Testing the Installer

1. Build the installer (see above)
2. Run `FlipKit-Setup-v3.0.0.exe` on a test machine or VM
3. Select installation options
4. Verify shortcuts are created
5. Launch FlipKit Desktop
6. Test Web/API servers if installed
7. Uninstall and verify clean removal

## Installer Output

**Location:** `releases\installer\FlipKit-Setup-v3.0.0.exe`

**Size:** ~150-200 MB (depending on components)

**Compression:** LZMA2 (maximum compression)

## Customization

To customize the installer:

1. Edit `flipkit-setup.iss`
2. Key settings:
   - `MyAppVersion` - Update version number
   - `AppId` - Unique GUID (do not change after first release)
   - `DefaultDirName` - Installation directory
   - `[Files]` section - Add/remove files to include
   - `[Icons]` section - Customize shortcuts
   - `[Messages]` section - Customize installer text

## Notes

- The installer requires admin privileges to install to Program Files
- User data is stored in `%LOCALAPPDATA%\FlipKit` (not removed on uninstall)
- The installer is 64-bit only (x64 architecture)
- Windows 10/11 recommended (Windows 8.1+ supported)
