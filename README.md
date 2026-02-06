# CardLister

A desktop application for sports card sellers that uses AI vision to scan card images, manage inventory, research pricing, and export Whatnot-compatible CSV files for bulk listing.

Built with **C# / .NET 8**, **Avalonia UI 11**, and the **MVVM pattern**.

## Features

- **AI Card Scanning** -- Drop a photo of any sports card (front + optional back) and AI vision extracts player name, year, set, brand, parallel, serial numbering, and more. Uses free OpenRouter vision models with automatic fallback across 11 models on rate limiting.
- **Variation Verification Pipeline** -- Cross-references AI scan results against local set checklists to catch hallucinated parallels, correct player names, and validate card numbers. Runs a targeted confirmation pass for ambiguous results.
- **Inventory Management** -- Browse, search, filter, and sort your card collection. Track card status (Draft, Listed, Sold), price staleness, and financial data.
- **Pricing Research** -- Opens pre-built Terapeak and eBay sold listing searches in your browser for quick comp research. Tracks estimated value, cost basis, and suggested list prices.
- **Image Hosting** -- Upload card images to ImgBB for free public URLs compatible with Whatnot listings.
- **Whatnot CSV Export** -- Generate properly formatted CSV files for Whatnot's bulk upload tool with all required columns pre-filled.
- **Financial Tracking** -- Track cost basis, sale price, fees, shipping, and net profit per card. Mark cards as sold with automatic fee calculation for Whatnot and eBay.
- **Reports** -- View inventory summary, total value, profit/loss, and price staleness at a glance.

## Screenshots

*Coming soon*

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- An [OpenRouter API key](https://openrouter.ai/keys) (free to sign up -- free vision models available)
- An [ImgBB API key](https://api.imgbb.com/) (optional, for image hosting)

### Build & Run

```bash
# Clone the repo
git clone https://github.com/mthous72/CardLister.git
cd CardLister

# Restore and run
dotnet run --project CardLister
```

### Publish as Single Executable

```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

The output executable will be in `CardLister/bin/Release/net8.0/win-x64/publish/`.

## How It Works

```
1. CAPTURE      Take a photo of your card (front + optional back)
       |
2. AI SCAN      AI vision extracts structured card data (player, year, set, parallel, etc.)
       |
3. VERIFY       Cross-reference against set checklists to validate identification
       |
4. REVIEW       Edit extracted data in a form, save to local SQLite database
       |
5. PRICE        Research comps via Terapeak/eBay, set your listing price
       |
6. EXPORT       Generate Whatnot CSV, upload to Seller Hub, publish
```

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Language | C# / .NET 8 |
| UI Framework | Avalonia UI 11 (Fluent theme) |
| Architecture | MVVM (CommunityToolkit.Mvvm) |
| Database | SQLite via Entity Framework Core |
| AI Vision | OpenRouter API (free vision models) |
| Image Hosting | ImgBB API |
| CSV Export | CsvHelper |
| DI | Microsoft.Extensions.DependencyInjection |

## Project Structure

```
CardLister/
+-- Models/           Domain entities (Card, PriceHistory, AppSettings, enums)
+-- ViewModels/       MVVM ViewModels with observable properties and commands
+-- Views/            Avalonia XAML views (no business logic in code-behind)
+-- Services/         Service interfaces and implementations (scanning, export, pricing, etc.)
+-- Data/             EF Core DbContext, seeders, schema management
+-- Converters/       XAML value converters (currency, status badges, confidence colors)
+-- Helpers/          Utility classes (FuzzyMatcher, PriceCalculator)
+-- Styles/           Shared Avalonia styles
+-- Docs/             Design specs and planning documents
```

## Configuration

On first launch, a setup wizard walks you through entering your API keys. Settings are stored locally in `config.json` in your app data folder. All card data stays on your machine in a local SQLite database.

## Supported Sports

- Football
- Baseball
- Basketball

## License

This project is for personal use.
