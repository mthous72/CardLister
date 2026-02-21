# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

FlipKit is a C# / .NET 8 application suite for sports card sellers, consisting of:

1. **FlipKit.Desktop** - Avalonia UI 11 desktop app (Windows/Mac/Linux) with full feature set
2. **FlipKit.Web** - ASP.NET Core 8.0 MVC web app for mobile access (phone/tablet browsers)
3. **FlipKit.Api** - Minimal API server (net9.0) for remote data access via Tailscale
4. **FlipKit.Core** - Shared business logic library (models, services, data access)

Desktop and Web share a single SQLite database with WAL mode for concurrent access. When using Tailscale for remote access, the Api server provides REST endpoints that Desktop and Web can consume instead of direct database access.

**Core Features:** AI vision scanning (OpenRouter API), inventory management, pricing research (eBay/Terapeak), Whatnot CSV export, sales tracking, financial reports.

**Current State:** v3.1.2 FlipKit Hub released. Unified package with Desktop app + embedded Web and API servers. Desktop and Web both feature-complete. Servers managed from Desktop Settings UI.

## Build & Run Commands

**Desktop App (Development):**
```bash
# Restore and build
dotnet restore
dotnet build

# Run desktop app (servers auto-start if configured)
dotnet run --project FlipKit.Desktop

# Build for release
dotnet build -c Release
```

**Web App (Development - Standalone):**
```bash
# Run web app standalone (development)
cd FlipKit.Web
dotnet run

# Run with specific URLs (for local network access)
dotnet run --urls "http://0.0.0.0:5000"
```

**API Server (Development - Standalone):**
```bash
# Run API server standalone
cd FlipKit.Api
dotnet run

# Database path configurable via environment variable
FLIPKIT_DB_PATH=/path/to/cards.db dotnet run
# Default: %LocalAppData%/FlipKit/cards.db
```

**Build FlipKit Hub Packages (Release):**
```bash
# Build unified packages for Windows and Linux
.\build-release.ps1 -Version 3.1.2

# Output: releases/FlipKit-Hub-Windows-x64-v3.1.2.zip
#         releases/FlipKit-Hub-Linux-x64-v3.1.2.zip
```

**All Projects:**
```bash
# Run tests (when test projects exist)
dotnet test

# Build entire solution
dotnet build FlipKit.sln
```

## Architecture

**Current: 4-Project Structure**

```
FlipKit.sln
│
├── FlipKit.Core/          # Shared business logic (net8.0 class library)
│   ├── Models/                # Domain entities (12 files)
│   │   ├── Card.cs, PriceHistory.cs, SetChecklist.cs, etc.
│   │   └── Enums/             # CardStatus, CostSource, ExportPlatform, Sport, VerificationConfidence
│   ├── Services/
│   │   ├── Interfaces/        # 14 service contracts
│   │   └── Implementations/   # 12 implementations
│   ├── Data/                  # FlipKitDbContext, migrations, seeders, SchemaUpdater
│   └── Helpers/               # FuzzyMatcher, PriceCalculator, DataAccessModeDetector, LegacyMigrator
│
├── FlipKit.Desktop/       # Avalonia UI app (net8.0 WinExe)
│   ├── Views/                 # 12 XAML views
│   ├── ViewModels/            # 14 ViewModels with [ObservableProperty]
│   ├── Services/              # 5 platform-specific services
│   │   ├── AvaloniaFileDialogService.cs
│   │   ├── AvaloniaNavigationService.cs
│   │   ├── JsonSettingsService.cs
│   │   ├── SystemBrowserService.cs
│   │   └── ServerManagementService.cs
│   ├── Converters/            # 9 XAML value converters
│   ├── Styles/                # AppStyles.axaml
│   ├── Assets/                # Icons, images, seed data JSON
│   ├── ViewLocator.cs
│   ├── App.axaml.cs
│   └── Program.cs
│
├── FlipKit.Web/           # ASP.NET Core MVC (net8.0 web app)
│   ├── Controllers/           # 7 controllers (Home, Inventory, Scan, Pricing, Export, Reports, Settings)
│   ├── Models/                # 14 ViewModels/DTOs for Razor views
│   ├── Views/
│   │   ├── Shared/            # _Layout.cshtml, Error.cshtml, partials
│   │   ├── Home/              # Dashboard, Privacy
│   │   ├── Inventory/         # Index, Details, Edit
│   │   ├── Scan/              # Index (camera upload), Results
│   │   ├── Pricing/           # Index (list), Research
│   │   ├── Export/            # Index, Preview
│   │   ├── Reports/           # Index (summary), Financial, Sales
│   │   └── Settings/          # Index (redirects to Desktop)
│   ├── Services/              # 4 platform-specific services
│   │   ├── WebFileUploadService.cs
│   │   ├── JavaScriptBrowserService.cs
│   │   ├── MvcNavigationService.cs
│   │   └── JsonSettingsService.cs
│   ├── wwwroot/               # Static files, CSS, JS
│   └── Program.cs             # DI, middleware, WAL mode setup
│
└── FlipKit.Api/           # Minimal API server (net9.0)
    └── Program.cs             # REST endpoints, CORS, health checks
```

### Dependency Flow

```
FlipKit.Desktop ─┐
                  ├─→ FlipKit.Core ←─ Shared database (WAL mode)
FlipKit.Web ─────┤
                  │
FlipKit.Api ─────┘
```

Desktop, Web, and Api all reference Core, but **never reference each other**.

### Data Access Modes

Both Desktop and Web support two data access modes, detected automatically by `DataAccessModeDetector`:

- **Local Mode** (default) - Direct SQLite access via `FlipKitDbContext`
- **Remote Mode** (via Tailscale) - HTTP calls to the Api server using `ApiCardRepository`

### API Server Architecture (FlipKit.Api)

**Minimal API design** (no controllers, endpoint mapping in Program.cs):
- CRUD: `/api/cards`, `/api/cards/{id}`
- Queries: `/api/cards/unpriced`, `/api/cards/stale`, `/api/cards/stats`
- Price history: `/api/cards/{id}/price-history`
- Reports: `/api/reports/sold`
- Sync: `/api/sync/status`, `/api/sync/cards`, `/api/sync/push`
- Health: `/`, `/health`
- CORS enabled for local network access
- Database path configurable via `FLIPKIT_DB_PATH` environment variable
- Listens on `http://0.0.0.0:5000`

### Web App Architecture (ASP.NET Core MVC)

**Data Flow:**
```
Browser → HTTP Request → Controller → Core Services → Database/APIs → View (Razor) → HTTP Response
```

**Key Patterns:**
- **Controllers** - Handle HTTP requests, call Core services, return views
- **ViewModels (DTOs)** - Simple data transfer objects for Razor views (no ObservableObject)
- **Views (Razor)** - Server-rendered HTML with Bootstrap 5, client-side JavaScript for interactivity
- **Shared Database** - SQLite with WAL mode enables concurrent Desktop + Web access without locking
- **Platform Services** - Web-specific implementations (file upload via IFormFile, browser navigation via response headers)
- **SettingsController** - Disabled in web; redirects to Desktop with info message

**DI Service Lifetimes:**
- **Singleton** - Stateless services (ISettingsService, IScannerService, IImageUploadService)
- **Scoped** - Services that depend on DbContext (ICardRepository, IPricerService, IExportService)
- **Transient** - Not used in web app

**Mobile Optimization:**
- Camera integration via `<input accept="image/*" capture="environment">`
- Bootstrap 5 responsive design (mobile-first)
- Touch-friendly UI elements
- JavaScript real-time calculators (profit calculator in pricing)

### Desktop MVVM Data Flow

```
View (XAML) → data binding → ViewModel (C#) → DI-injected services → Data/APIs
```

- **Views** are pure XAML with declarative bindings. No business logic in code-behind.
- **ViewModels** use CommunityToolkit.Mvvm source generators: `[ObservableProperty]` for reactive properties, `[RelayCommand]` for async commands.
- **Services** are accessed via interfaces injected through constructors.
- **Navigation** is ViewModel-first: `MainWindowViewModel.CurrentPage` holds the active ViewModel; `ViewLocator` resolves the matching View.
- **Server Management** - Desktop manages Web and API server processes via `ServerManagementService`, with tray icon controls and auto-start support.
- **Smart Mode Detection** - Automatically switches between Local SQLite and Remote API mode (Tailscale).

### ViewLocator Convention

The `ViewLocator` maps ViewModel types to View types by replacing `"ViewModel"` with `"View"` in the fully qualified type name.

## Key Dependencies

| Package | Version | Project | Purpose |
|---------|---------|---------|---------|
| Avalonia | 11.3.11 | Desktop | Cross-platform UI framework |
| Avalonia.Themes.Fluent | 11.3.11 | Desktop | Modern Fluent theme |
| Avalonia.Controls.DataGrid | 11.3.11 | Desktop | DataGrid control |
| Avalonia.Fonts.Inter | 11.3.11 | Desktop | Inter font family |
| CommunityToolkit.Mvvm | 8.2.1 | Desktop | MVVM source generators |
| Microsoft.EntityFrameworkCore.Sqlite | 8.0.11 | Core, Desktop, Api | SQLite database with EF Core |
| CsvHelper | 33.0.1 | Core, Desktop | CSV export for Whatnot |
| HtmlAgilityPack | 1.11.71 / 1.12.4 | Core / Desktop | HTML parsing (sold price scraping) |
| Serilog | 4.1.0 | Core | Structured logging |
| Serilog.Extensions.Logging | 8.0.0 | Desktop | Serilog integration with MS logging |
| Serilog.Sinks.File | 6.0.0 | Desktop | File logging sink |
| Microsoft.Extensions.DependencyInjection | 8.0.1 | Core, Desktop | DI container |
| Microsoft.Extensions.Http | 8.0.1 | Core, Desktop | HttpClient factory |
| Microsoft.Extensions.Logging | 8.0.1 | Core, Desktop | Logging abstractions |
| System.Text.Json | 8.0.5 | Core | JSON serialization |
| QRCoder | 1.6.0 | Desktop | QR code generation (network access) |
| Microsoft.AspNetCore.OpenApi | 9.0.11 | Api | OpenAPI/Swagger support |

## Important Conventions

- **Nullable reference types enabled** (`<Nullable>enable</Nullable>`) — all types must be explicitly nullable or non-nullable.
- **Compiled bindings by default** (`<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>`) in Desktop.
- Use `decimal` for all money fields, `DateTime` for all date fields.
- Enums stored as strings in the database.
- All I/O operations must be `async Task`.
- Avalonia `DataAnnotationsValidationPlugin` is disabled in `App.axaml.cs` to avoid conflicts with CommunityToolkit validation.
- **DbContext class** is `FlipKitDbContext` (defined in `CardListerDbContext.cs` — legacy filename from rebrand).
- **Legacy Migration** - `LegacyMigrator` handles one-time data migration from CardLister to FlipKit folder.
- **Api targets net9.0** while Core, Desktop, and Web target net8.0.

## Implementation Status

### Completed Features (Production Ready)

**Core Workflow:**
- AI vision scanning via OpenRouter (11 free models supported)
- Variation verification against checklist database with fuzzy matching
- Single-card and bulk scanning workflows
- Inventory management (CRUD, filtering, bulk operations, search)
- Pricing research with browser integration (Terapeak/eBay)
- Whatnot CSV export with validation
- ImgBB image hosting integration
- Sales tracking and profitability reports
- Financial reporting by date range

**Advanced Features:**
- Graded card support (PSA, BGS, CGC, CCG, SGC)
- Checklist learning system (improves from saved cards)
- Checklist CSV import and editing
- Price staleness tracking with visual indicators
- "Mark as Sold" workflow with profit calculation
- Setup wizard for first-run configuration
- Settings persistence (JSON)
- Logging (Serilog to file)
- Sold price research (130point.com scraping via HtmlAgilityPack)
- Title template service for standardized card titles

**FlipKit Hub (Unified Package):**
- Desktop app with embedded Web and API server management
- Server auto-start on Desktop launch
- Tray icon with server controls
- QR code generation for mobile access
- Health check endpoints for server monitoring
- CardLister-to-FlipKit legacy data migration

**Technical Implementation (Desktop):**
- 14 ViewModels with MVVM pattern
- 12 Views with Avalonia UI
- 14 service interfaces, 12 implementations in Core + 5 Desktop-specific services
- SQLite database with EF Core (88 fields per card)
- Seed data system for checklists
- 9 custom XAML value converters
- Fuzzy matching for verification (0.85/0.7 thresholds)
- Rate limiting for free-tier AI models

**Web Application:**
- 4-project architecture (Core, Desktop, Web, Api)
- ASP.NET Core 8.0 MVC with Bootstrap 5
- 7 controllers (Home, Inventory, Scan, Pricing, Export, Reports, Settings)
- 20 Razor views with responsive design
- 14 ViewModels/DTOs
- Mobile camera integration for card scanning
- Shared SQLite database with WAL mode (concurrent access)
- Platform-specific service implementations
- Real-time JavaScript calculators (profit calculator)
- Full CRUD inventory management
- CSV export for Whatnot
- Sales and financial analytics

**API Server:**
- Minimal API with full CRUD endpoints
- Specialized query endpoints (unpriced, stale, stats)
- Price history management
- Sold cards reporting with date filtering
- Sync endpoints for backwards compatibility
- OpenAPI documentation
- CORS for local network access

### Future Roadmap

**High Priority:**
- Web app authentication (multi-user support)
- Unit and integration tests
- Progressive Web App (PWA) - install web app on phone home screen
- Real-time sync between Desktop and Web (SignalR)

**Medium Priority:**
- Bulk scan from web interface
- Additional export formats (eBay, COMC)
- Performance optimizations for large inventories (1000+ cards)
- Dark theme support (Desktop and Web)
- Cloud sync / backup

**Low Priority:**
- Automated price scraping (replace manual browser lookup)
- Barcode scanning
- Price alerts/notifications

See `Docs/17-FUTURE-ROADMAP.md` for detailed planning.

## Planning Documents

Comprehensive specs are in `Docs/`. Most are now implemented, use as reference for modifications:

| Doc | Status | Content |
|-----|--------|---------|
| `00-PROGRAM-OVERVIEW.md` | 📖 Reference | High-level program overview |
| `01-PROJECT-PLAN.md` | 📝 Updated | Architecture, tech stack, development phases |
| `02-DATABASE-SCHEMA.md` | ✅ Implemented | EF Core entities (Card, PriceHistory, SetChecklist), enums |
| `03-OPENROUTER-INTEGRATION.md` | ✅ Implemented | AI vision API setup and prompts (11 models) |
| `04-WHATNOT-CSV-FORMAT.md` | ✅ Implemented | Export CSV column mapping |
| `05-PRICING-RESEARCH.md` | ⚠️ Partial | Terapeak/eBay URL construction (browser links, no scraping) |
| `06-IMAGE-HOSTING.md` | ✅ Implemented | ImgBB API integration |
| `07-CLAUDE-CODE-GUIDE.md` | 📝 Updated | Working with existing codebase guide |
| `08-CARD-TERMINOLOGY.md` | 📖 Reference | Sports card domain reference |
| `09-EBAY-API.md` | 📖 Reference | eBay API integration notes |
| `10-GUI-ARCHITECTURE.md` | ✅ Implemented | Detailed Avalonia MVVM patterns, DI setup, view specs |
| `10-GUI-OPTIONS.md` | 📖 Reference | GUI framework options analysis |
| `11-UX-DESIGN.md` | 📖 Reference | UX design guidelines |
| `12-INSTALL-GUIDE.md` | 📖 Reference | Installation guide |
| `13-INVENTORY-TRACKING.md` | ✅ Implemented | Price staleness and financial tracking |
| `14-VARIATION-VERIFICATION.md` | ✅ Implemented | Checklist-based verification system |
| `15-VERIFICATION-BUILD-GUIDE.md` | 📖 Reference | Verification feature build guide |
| `16-CHECKLIST-DATA-SPEC.md` | ✅ Implemented | Checklist data structure and seeding |
| `17-FUTURE-ROADMAP.md` | 📖 Reference | Future feature planning and priorities |
| `18-PHASE1-COMPLETION-SUMMARY.md` | ✅ Complete | Core library extraction and refactor summary |
| `19-TESTING-CHECKLIST-PHASE1.md` | ✅ Complete | Comprehensive testing checklist for Phase 1 |
| `20-PHASE2-COMPLETION-SUMMARY.md` | ✅ Complete | Web app foundation and feature implementation |
| `21-PHASE3-TESTING-PLAN.md` | ✅ Complete | Functional testing, mobile, performance, security |
| `22-PHASE3-PROGRESS-SUMMARY.md` | ✅ Complete | Phase 3 status and progress tracking |
| `23-FUNCTIONAL-TEST-RESULTS.md` | ✅ Complete | All page load tests passed (8/8) |
| `24-PHASE3-COMPLETION-SUMMARY.md` | ✅ Complete | Phase 3 completion summary |
| `25-DISTRIBUTION-PACKAGING.md` | ✅ Complete | Release packaging and distribution |
| `DEPLOYMENT-GUIDE.md` | 📖 Reference | Web app deployment and network setup |
| `HUB-ARCHITECTURE.md` | 📖 Reference | FlipKit Hub architecture and server management |
| `USER-GUIDE.md` | 📖 Reference | Desktop app user guide with screenshots |
| `WEB-USER-GUIDE.md` | 📖 Reference | Web app user guide for mobile access |

## Git Branching Workflow

- **Never commit directly to `master`.** All work must be done on feature/fix branches.
- **Branch naming:** `feature/<short-name>` for new features, `fix/<short-name>` for bug fixes (e.g., `feature/graded-cards`, `fix/date-picker-type`).
- **Create the branch before making changes:** `git checkout -b feature/<name>` from an up-to-date `master`.
- **Merge to master only after verification:** `dotnet build` passes with 0 errors, and the feature has been manually tested.
- **Delete the branch after merging** to keep the repo clean.

## Common Troubleshooting

- **View not found at runtime:** Check that the ViewModel class name matches the View name via the ViewLocator convention.
- **Binding not working:** Ensure properties use `[ObservableProperty]` (generates public property from `_camelCase` field) or manually raise `PropertyChanged`.
- **Command not firing:** Check `CanExecute` logic and ensure dependent properties call `OnPropertyChanged` when they change.
- **EF Core "no migrations":** Run `dotnet ef migrations add <Name>` from the Infrastructure project with `--startup-project` pointing to the App project.
- **OpenRouter JSON parse fails:** Strip markdown code blocks (` ```json `) from API response before deserializing.
- **DbContext file mismatch:** The class is `FlipKitDbContext` but lives in `CardListerDbContext.cs` (legacy filename from CardLister rebrand).
- **Data access mode issues:** Check `DataAccessModeDetector` — it auto-detects Local vs Remote mode based on Tailscale availability.
- **Server management:** Web and API servers are managed as child processes by `ServerManagementService` in Desktop. Check server health via `/health` endpoints.
