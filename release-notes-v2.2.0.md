# 🎯 What's New in v2.2.0

## Major Features

**Smart Hybrid Data Access**
- Automatic detection of local vs remote mode
- Fast local SQLite access when on same computer
- API-based access when remote via Tailscale
- Zero configuration - works automatically!

**Full Mobile Inventory Management**
- Complete inventory features in web app
- Browse, search, filter cards from phone
- Edit card details and pricing from mobile
- Delete cards with confirmation
- CSV export directly from phone
- Sales reports and analytics

**Tailscale Network Support**
- Access your cards from anywhere on private Tailscale network
- Secure, encrypted connections
- No cloud hosting fees
- Works with Desktop, Web, and API

**API Server (NEW!)**
- RESTful data access API
- 11+ endpoints for complete CRUD operations
- Health check endpoint for easy testing
- Perfect for remote Desktop/Web app access
- Launcher scripts included for easy startup

## Technical Improvements

- `ApiCardRepository` for HTTP-based remote access
- `DataAccessModeDetector` for automatic mode selection
- Proper resource cleanup on shutdown (no lingering processes)
- Enhanced error handling and logging
- Memory leak fixes (proper event handler cleanup)
- Removed sync complexity (real-time API instead)

## Breaking Changes

- Removed old Tailscale sync feature (replaced with hybrid mode)
- Settings UI simplified - no more sync checkboxes

---

# 📱 Mobile Workflow Now Complete!

With v2.2.0, you can manage your entire card inventory from your phone:

1. **Scan** → Use camera to scan cards
2. **Price** → Research and set prices
3. **Manage** → Edit, search, filter inventory
4. **Export** → Generate Whatnot CSV
5. **Track** → View sales reports

All from your Android or iOS browser via Tailscale! 🚀

---

# 📥 Downloads

## Desktop Application

For full-featured desktop experience with bulk scanning:

- **Windows (x64)** - Extract and double-click `CardLister.exe`
- **macOS Intel** - Extract and run `./CardLister` from terminal
- **macOS Apple Silicon** - Extract and run `./CardLister` from terminal

## Web Application

For mobile access - run on your computer, access from phone:

- **Windows (x64)** - Extract and double-click `StartWeb.bat`
- **macOS Intel** - Extract and run `./start-web.sh`
- **macOS Apple Silicon** - Extract and run `./start-web.sh`
- **Linux (x64)** - Extract and run `./start-web.sh`

## API Server (NEW!)

For remote Desktop/Web app access via Tailscale:

- **Windows (x64)** - Extract and double-click `StartAPI.bat`
- **macOS Intel** - Extract and run `./start-api.sh`
- **macOS Apple Silicon** - Extract and run `./start-api.sh`
- **Linux (x64)** - Extract and run `./start-api.sh`

---

# 🆕 First Time Setup

1. **Desktop/Web:** Extract and run - includes launcher scripts
2. **API (Optional):** Only needed for remote access via Tailscale
3. **Configure:** Enter OpenRouter and ImgBB API keys in Settings
4. **Tailscale (Optional):** Install for remote access features

See [README](https://github.com/mthous72/CardLister#readme) for detailed setup instructions.

---

# 📚 Documentation

- [README](https://github.com/mthous72/CardLister#readme) - Full feature list and setup
- [TAILSCALE-SYNC-GUIDE.md](https://github.com/mthous72/CardLister/blob/master/TAILSCALE-SYNC-GUIDE.md) - Remote access setup
- [WEB-USER-GUIDE.md](https://github.com/mthous72/CardLister/blob/master/Docs/WEB-USER-GUIDE.md) - Mobile app usage

---

# 🙏 Feedback & Issues

Found a bug or have a suggestion? [Open an issue](https://github.com/mthous72/CardLister/issues)!
