# 🎉 FlipKit v3.0.0 - Major Rebrand Release!

**Release Date:** February 10, 2026

---

## 🚀 What's New

### Brand New Identity: FlipKit

CardLister has been rebranded to **FlipKit**! The new name is action-oriented and speaks directly to card sellers and flippers. This major release brings a fresh brand identity while maintaining all the features you love.

### 🔄 Automatic Data Migration

Existing CardLister users rejoice! Your data automatically migrates to FlipKit on first launch:

- **Seamless upgrade** - No manual steps required
- **Preserves everything** - Database, settings, logs all migrated
- **Safe migration** - Original CardLister data remains untouched as backup
- Works across all platforms - Desktop, Web, and API

### 📦 Installation Path Changes

- **Old:** `%LOCALAPPDATA%\CardLister`
- **New:** `%LOCALAPPDATA%\FlipKit`

All executable names, namespaces, and project names updated to reflect the rebrand.

---

## 📥 Downloads

### Windows Users

**Recommended:** Download the all-in-one Windows Installer (coming soon in v3.0.1)

**Alternative:** Download individual packages below:

- **Desktop App:** `FlipKit-Desktop-Windows-x64-v3.0.0.zip`
- **Web Server:** `FlipKit-Web-Windows-x64-v3.0.0.zip` (for mobile access)
- **API Server:** `FlipKit-API-Windows-x64-v3.0.0.zip` (for remote access)

### macOS Users

- **Desktop App (Intel):** `FlipKit-Desktop-macOS-Intel-v3.0.0.zip`
- **Desktop App (ARM):** `FlipKit-Desktop-macOS-ARM-v3.0.0.zip`
- **Web Server (Intel):** `FlipKit-Web-macOS-Intel-v3.0.0.tar.gz`
- **Web Server (ARM):** `FlipKit-Web-macOS-ARM-v3.0.0.tar.gz`
- **API Server (Intel):** `FlipKit-API-macOS-Intel-v3.0.0.tar.gz`
- **API Server (ARM):** `FlipKit-API-macOS-ARM-v3.0.0.tar.gz`

### Linux Users

- **Web Server:** `FlipKit-Web-Linux-x64-v3.0.0.tar.gz`
- **API Server:** `FlipKit-API-Linux-x64-v3.0.0.tar.gz`

---

## ✨ What Changed

### Product Branding

- ✅ All namespaces: `CardLister.*` → `FlipKit.*`
- ✅ Database path: `%LOCALAPPDATA%\CardLister` → `%LOCALAPPDATA%\FlipKit`
- ✅ Executable names: `CardLister.Desktop.exe` → `FlipKit.Desktop.exe`
- ✅ Log file prefix: `cardlister-` → `flipkit-`
- ✅ All documentation and UI updated to FlipKit branding

### Technical Changes

- ✅ Added `FlipKit.Core.Helpers.LegacyMigrator` for automatic data migration
- ✅ Migration integrated into Desktop, Web, and API startup sequences
- ✅ Updated build script to generate FlipKit-branded packages
- ✅ Solution and project files renamed

### No Feature Changes

**Important:** This is a pure rebrand. All v2.2.1 features remain identical:

- AI vision scanning (11 free models)
- Inventory management
- Pricing research (eBay/Terapeak)
- Whatnot CSV export
- Sales tracking & reports
- Graded card support
- Checklist verification
- Concurrent Desktop + Web access (WAL mode)
- Smart hybrid mode (local database or remote API)

---

## 🔄 Upgrading from CardLister v2.x

### Automatic Migration (Recommended)

1. Download and extract FlipKit v3.0.0
2. Launch FlipKit (Desktop, Web, or API)
3. Migration happens automatically on first run
4. Your CardLister data is preserved as backup

### Manual Migration (Advanced)

If you prefer manual control:

1. Close all CardLister apps
2. Copy `%LOCALAPPDATA%\CardLister\*` to `%LOCALAPPDATA%\FlipKit\`
3. Launch FlipKit

---

## ⚠️ Breaking Changes

### Version Bump

- **Old:** v2.2.1
- **New:** v3.0.0

Semantic versioning justifies the major version bump due to:

- Product name change
- Installation path change
- Executable name changes

### No API Compatibility Issues

Since FlipKit is a standalone desktop/web app (not a library), there are no API compatibility concerns for users.

---

## 📝 Known Issues

None at this time. This release has been thoroughly tested against the v2.2.1 codebase.

---

## 🙏 Credits

Special thanks to all CardLister users who provided feedback and helped shape the rebranding decision. Your input was invaluable!

---

## 📖 Documentation

- [User Guide](Docs/USER-GUIDE.md) - Desktop app walkthrough
- [Web User Guide](Docs/WEB-USER-GUIDE.md) - Mobile web app guide
- [Deployment Guide](Docs/DEPLOYMENT-GUIDE.md) - Web/API server setup
- [CLAUDE.md](CLAUDE.md) - Developer reference

---

## 🐛 Reporting Issues

Found a bug? Please report it on GitHub: https://github.com/YOUR_USERNAME/FlipKit/issues

---

**Enjoy the new FlipKit experience!** 🎉
