# eBay Browse API Integration Plan

## Status: ACTIVE (February 2026)

**Branch:** `feature/ebay-browse-api`

## Context

CardLister currently requires manual pricing research via Terapeak and eBay sold listings. While the Point130SoldPriceService was implemented for automated sold price scraping, it proved "not very effective in practice" and was shelved.

**New Approach:** eBay Browse API for Active Listing Comps

## Why This Matters

**Active listings are more useful than sold data for competitive pricing:**
- Shows current market prices (not historical)
- Reveals what competitors are listing at RIGHT NOW
- Helps set competitive prices to sell faster
- Free for sellers: 5,000 API calls/day
- Official eBay API (no scraping/ToS concerns)
- Supports both keyword search AND image-based search

**The Point130 Problem:**
- Scraped sold prices (historical data)
- Fragile (site changes break scraper)
- ToS violations
- Limited data quality

**The Browse API Solution:**
- Official eBay API with OAuth
- Active listings (current market state)
- Image search capability (drag card photo → find matches)
- Stable, documented, supported
- Free tier: 5,000 calls/day

## Architecture Pattern

Based on Card-Database project's TCGplayer integration:

```
Request → Parse → Cache → Display
```

### Service Architecture

```csharp
public interface IActiveListingService
{
    // Search for active listings by keyword
    Task<List<ActiveListingRecord>> SearchListingsAsync(Card card);

    // Search for active listings by image (future feature)
    Task<List<ActiveListingRecord>> SearchByImageAsync(string imagePath);

    // Get cached listings from local database
    Task<List<ActiveListingRecord>> GetCachedListingsAsync(Card card, int daysOld = 7);

    // Calculate suggested price from active listings
    PriceLookupResult CalculateMarketValue(List<ActiveListingRecord> listings, Card card);
}

public class EbayBrowseService : IActiveListingService
{
    private readonly HttpClient _httpClient;
    private readonly CardListerDbContext _dbContext;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<EbayBrowseService> _logger;

    // OAuth token management
    private string? _accessToken;
    private DateTime _tokenExpiry;

    // Rate limiting (5000 calls/day = ~208/hour)
    private static readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private static DateTime _lastRequestTime = DateTime.MinValue;
}
```

## Database Schema

### New Table: `ActiveListingRecords`

```sql
CREATE TABLE ActiveListingRecords (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,

    -- Card identification (for matching)
    PlayerName TEXT NOT NULL,
    Year INTEGER,
    Manufacturer TEXT,
    Brand TEXT,
    CardNumber TEXT,
    ParallelName TEXT,

    -- Condition/grading
    Condition TEXT,
    IsGraded BOOLEAN DEFAULT 0,
    GradeCompany TEXT,
    GradeValue TEXT,

    -- Listing details
    ListingPrice DECIMAL(10,2) NOT NULL,
    Currency TEXT DEFAULT 'USD',
    Title TEXT,
    ListingUrl TEXT,
    ItemId TEXT,                    -- eBay item ID
    SellerUsername TEXT,

    -- Listing metadata
    ListingFormat TEXT,             -- Auction, Buy It Now, Best Offer
    BidsCount INTEGER,              -- For auctions
    WatchersCount INTEGER,
    TimeLeft TEXT,                  -- "3d 5h 12m"
    EndTime DATETIME,
    ShippingCost DECIMAL(10,2),
    ShippingType TEXT,              -- Free, Calculated, Flat

    -- Image URLs
    ImageUrl TEXT,
    ThumbnailUrl TEXT,

    -- API metadata
    FetchedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    Sport TEXT,

    -- Search query used (for debugging)
    SearchQuery TEXT
);

CREATE INDEX idx_activelisting_lookup ON ActiveListingRecords(PlayerName, Year, Brand, Sport);
CREATE INDEX idx_activelisting_fetched ON ActiveListingRecords(FetchedAt);
CREATE INDEX idx_activelisting_itemid ON ActiveListingRecords(ItemId);
```

**Key differences from SoldPriceRecord:**
- `ListingPrice` instead of `SoldPrice`
- `TimeLeft` and `EndTime` for auction tracking
- `WatchersCount` to gauge interest
- `ItemId` for deduplication
- `ImageUrl` for visual confirmation
- No `SaleType` (all are active, format is in `ListingFormat`)

## eBay Browse API Integration

### API Endpoints

**Base URL:** `https://api.ebay.com/buy/browse/v1`

**Key Endpoints:**
1. **Search by keyword:** `GET /item_summary/search`
2. **Get item details:** `GET /item/{item_id}`
3. **Search by image:** `POST /item_summary/search_by_image` (future feature)

### Authentication Flow

**OAuth 2.0 Client Credentials Flow:**

```csharp
private async Task<string> GetAccessTokenAsync()
{
    // Check if token is still valid
    if (_accessToken != null && DateTime.UtcNow < _tokenExpiry)
        return _accessToken;

    // Get credentials from settings
    var settings = _settingsService.Load();
    var clientId = settings.EbayClientId;
    var clientSecret = settings.EbayClientSecret;

    // Request new token
    var authValue = Convert.ToBase64String(
        Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

    var request = new HttpRequestMessage(HttpMethod.Post,
        "https://api.ebay.com/identity/v1/oauth2/token");
    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);
    request.Content = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("grant_type", "client_credentials"),
        new KeyValuePair<string, string>("scope", "https://api.ebay.com/oauth/api_scope")
    });

    var response = await _httpClient.SendAsync(request);
    var json = await response.Content.ReadAsStringAsync();
    var tokenData = JsonSerializer.Deserialize<EbayTokenResponse>(json);

    _accessToken = tokenData.AccessToken;
    _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn - 300); // 5 min buffer

    return _accessToken;
}
```

### Search Implementation

```csharp
public async Task<List<ActiveListingRecord>> SearchListingsAsync(Card card)
{
    // Check cache first (7 days freshness)
    var cached = await GetCachedListingsAsync(card, daysOld: 7);
    if (cached.Count >= 10) // Enough data
    {
        _logger.LogInformation("Using cached active listings for {Player}", card.PlayerName);
        return cached;
    }

    // Build search query (reuse existing logic from PricerService)
    var searchQuery = BuildSearchQuery(card);

    // Rate limiting
    await _rateLimiter.WaitAsync();
    try
    {
        var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
        if (timeSinceLastRequest.TotalMilliseconds < 500) // 2 req/sec max
        {
            await Task.Delay(500 - (int)timeSinceLastRequest.TotalMilliseconds);
        }

        // Get OAuth token
        var token = await GetAccessTokenAsync();

        // Build request
        var requestUrl = "https://api.ebay.com/buy/browse/v1/item_summary/search?" +
            $"q={Uri.EscapeDataString(searchQuery)}&" +
            "category_ids=261328&" +  // Sports Trading Cards category
            "limit=50&" +
            "filter=buyingOptions:{FIXED_PRICE|AUCTION}";

        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-EBAY-C-MARKETPLACE-ID", "EBAY_US");

        var response = await _httpClient.SendAsync(request);
        _lastRequestTime = DateTime.UtcNow;

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("eBay API error: {Status}", response.StatusCode);
            return cached; // Fallback to cache
        }

        var json = await response.Content.ReadAsStringAsync();
        var searchResults = JsonSerializer.Deserialize<EbaySearchResponse>(json);

        // Parse and store results
        var listings = ParseListings(searchResults, card, searchQuery);

        // Save to database (deduplicate by ItemId)
        foreach (var listing in listings)
        {
            var existing = await _dbContext.ActiveListingRecords
                .FirstOrDefaultAsync(l => l.ItemId == listing.ItemId);

            if (existing == null)
            {
                _dbContext.ActiveListingRecords.Add(listing);
            }
            else
            {
                // Update existing record (price may have changed)
                existing.ListingPrice = listing.ListingPrice;
                existing.TimeLeft = listing.TimeLeft;
                existing.BidsCount = listing.BidsCount;
                existing.FetchedAt = DateTime.UtcNow;
            }
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Fetched {Count} active listings for {Player}",
            listings.Count, card.PlayerName);

        return listings;
    }
    finally
    {
        _rateLimiter.Release();
    }
}

private string BuildSearchQuery(Card card)
{
    var settings = _settingsService.Load();
    var titleService = new TitleTemplateService();

    // Reuse eBay search template from settings
    // Exclude CardNumber and Serial for broader results
    return titleService.GenerateTitle(card, settings.EbaySearchTemplate);
}

private List<ActiveListingRecord> ParseListings(
    EbaySearchResponse response, Card card, string searchQuery)
{
    var listings = new List<ActiveListingRecord>();

    if (response.ItemSummaries == null)
        return listings;

    foreach (var item in response.ItemSummaries)
    {
        try
        {
            var listing = new ActiveListingRecord
            {
                PlayerName = card.PlayerName,
                Year = card.Year,
                Manufacturer = card.Manufacturer,
                Brand = card.Brand,
                Sport = card.Sport.ToString(),
                SearchQuery = searchQuery,

                // Listing data
                ItemId = item.ItemId,
                Title = item.Title,
                ListingUrl = item.ItemWebUrl,
                ListingPrice = ParsePrice(item.Price),
                Currency = item.Price?.Currency ?? "USD",

                // Format/type
                ListingFormat = item.BuyingOptions?.Contains("AUCTION") == true
                    ? "Auction" : "Buy It Now",
                BidsCount = item.BidCount ?? 0,

                // Timing
                TimeLeft = item.TimeLeft,
                EndTime = ParseEndTime(item.ItemEndDate),

                // Shipping
                ShippingCost = ParseShippingCost(item.ShippingOptions),
                ShippingType = ParseShippingType(item.ShippingOptions),

                // Images
                ImageUrl = item.Image?.ImageUrl,
                ThumbnailUrl = item.ThumbnailImages?.FirstOrDefault()?.ImageUrl,

                // Metadata
                SellerUsername = item.Seller?.Username,
                FetchedAt = DateTime.UtcNow
            };

            listings.Add(listing);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse listing: {Title}", item.Title);
        }
    }

    return listings;
}
```

## Price Calculation Strategy

```csharp
public PriceLookupResult CalculateMarketValue(
    List<ActiveListingRecord> listings, Card card)
{
    if (!listings.Any())
    {
        return new PriceLookupResult
        {
            Success = false,
            Confidence = PriceConfidence.None,
            Source = "eBay Active (no matches)"
        };
    }

    // Filter to relevant listings (fuzzy match on player, parallel, grade)
    var relevantListings = FilterRelevantListings(listings, card);

    if (!relevantListings.Any())
    {
        return new PriceLookupResult
        {
            Success = false,
            Confidence = PriceConfidence.Low,
            Source = $"eBay Active ({listings.Count} listings, 0 relevant)"
        };
    }

    // Separate auctions and fixed-price listings
    var auctions = relevantListings.Where(l => l.ListingFormat == "Auction").ToList();
    var fixedPrice = relevantListings.Where(l => l.ListingFormat == "Buy It Now").ToList();

    // Calculate statistics
    var allPrices = relevantListings.Select(l => (double)l.ListingPrice).ToList();

    // Remove outliers (>2 std dev from mean)
    var mean = allPrices.Average();
    var stdDev = Math.Sqrt(allPrices.Average(p => Math.Pow(p - mean, 2)));
    var filtered = allPrices.Where(p => Math.Abs(p - mean) <= 2 * stdDev).ToList();

    var median = filtered.OrderBy(p => p).ElementAt(filtered.Count / 2);
    var low = filtered.Min();
    var high = filtered.Max();

    // Determine confidence
    var confidence = filtered.Count >= 10 ? PriceConfidence.High :
                     filtered.Count >= 5 ? PriceConfidence.Medium :
                     PriceConfidence.Low;

    return new PriceLookupResult
    {
        Success = true,
        MedianPrice = (decimal)median,
        AveragePrice = (decimal)filtered.Average(),
        LowPrice = (decimal)low,
        HighPrice = (decimal)high,
        SampleSize = filtered.Count,
        Confidence = confidence,
        Source = $"eBay Active ({filtered.Count} listings: " +
                 $"{fixedPrice.Count} Buy It Now, {auctions.Count} Auction)",

        // Additional metadata
        Metadata = new Dictionary<string, object>
        {
            { "AuctionCount", auctions.Count },
            { "FixedPriceCount", fixedPrice.Count },
            { "AuctionMedian", auctions.Any() ?
                auctions.Select(a => a.ListingPrice).Median() : 0 },
            { "FixedPriceMedian", fixedPrice.Any() ?
                fixedPrice.Select(f => f.ListingPrice).Median() : 0 }
        }
    };
}
```

## UI Integration

### PricingView Updates

Add a new section for active listings alongside Terapeak/eBay sold:

```xml
<!-- Active eBay Listings -->
<Border BorderBrush="{DynamicResource SystemControlHighlightAccentBrush}"
        BorderThickness="2" CornerRadius="8" Padding="16" Margin="0,0,0,16"
        Background="{DynamicResource SystemControlBackgroundAltHighBrush}">
    <StackPanel Spacing="12">
        <TextBlock Text="Active eBay Listings" FontWeight="SemiBold" FontSize="16"/>

        <TextBlock TextWrapping="Wrap" Opacity="0.8" FontSize="12">
            See what similar cards are currently listed for on eBay (active comps).
            Updates automatically, refreshes every 7 days.
        </TextBlock>

        <StackPanel Orientation="Horizontal" Spacing="12">
            <Button Content="Get Active Listings"
                    Command="{Binding GetActiveListingsCommand}"
                    IsEnabled="{Binding !IsLoadingActiveListings}"
                    Padding="20,10"
                    Classes="accent"/>

            <!-- Loading indicator -->
            <StackPanel Orientation="Horizontal" Spacing="8"
                        IsVisible="{Binding IsLoadingActiveListings}"
                        VerticalAlignment="Center">
                <TextBlock Text="⏳" FontSize="18"/>
                <TextBlock Text="Fetching from eBay API..." VerticalAlignment="Center"/>
            </StackPanel>
        </StackPanel>

        <!-- Results display -->
        <Border IsVisible="{Binding ActiveListingResult.Success}"
                Background="{DynamicResource SystemControlBackgroundBaseLowBrush}"
                Padding="12" CornerRadius="4" BorderThickness="1"
                BorderBrush="{DynamicResource SystemControlForegroundBaseMediumLowBrush}">
            <StackPanel Spacing="8">
                <StackPanel Orientation="Horizontal" Spacing="8">
                    <TextBlock Text="Median Active Price:" FontWeight="SemiBold"/>
                    <TextBlock Text="{Binding ActiveListingResult.MedianPrice, StringFormat='{}{0:C2}'}"
                               FontSize="18" FontWeight="Bold"
                               Foreground="{DynamicResource SystemControlHighlightAccentBrush}"/>
                </StackPanel>

                <Grid ColumnDefinitions="Auto,*" RowDefinitions="Auto,Auto,Auto,Auto"
                      ColumnSpacing="12" RowSpacing="4">
                    <TextBlock Grid.Row="0" Grid.Column="0" Text="Range:" Opacity="0.7"/>
                    <TextBlock Grid.Row="0" Grid.Column="1" Opacity="0.7">
                        <Run Text="{Binding ActiveListingResult.LowPrice, StringFormat='{}{0:C2}'}"/>
                        <Run Text=" - "/>
                        <Run Text="{Binding ActiveListingResult.HighPrice, StringFormat='{}{0:C2}'}"/>
                    </TextBlock>

                    <TextBlock Grid.Row="1" Grid.Column="0" Text="Sample:" Opacity="0.7"/>
                    <TextBlock Grid.Row="1" Grid.Column="1" Opacity="0.7">
                        <Run Text="{Binding ActiveListingResult.SampleSize}"/>
                        <Run Text=" active listings"/>
                    </TextBlock>

                    <TextBlock Grid.Row="2" Grid.Column="0" Text="Breakdown:" Opacity="0.7"/>
                    <TextBlock Grid.Row="2" Grid.Column="1" Opacity="0.7">
                        <Run Text="{Binding ActiveListingResult.FixedPriceCount}"/>
                        <Run Text=" Buy It Now, "/>
                        <Run Text="{Binding ActiveListingResult.AuctionCount}"/>
                        <Run Text=" Auction"/>
                    </TextBlock>

                    <TextBlock Grid.Row="3" Grid.Column="0" Text="Confidence:" Opacity="0.7"/>
                    <TextBlock Grid.Row="3" Grid.Column="1"
                               Text="{Binding ActiveListingResult.Confidence}"
                               Opacity="0.7"/>
                </Grid>

                <!-- Link to view all listings -->
                <Button Content="View All Active Listings on eBay"
                        Command="{Binding OpenActiveListingsCommand}"
                        HorizontalAlignment="Left"
                        Padding="12,6" Margin="0,8,0,0"/>

                <TextBlock Text="{Binding ActiveListingResult.Source}"
                           FontSize="11" Opacity="0.6" FontStyle="Italic"/>
            </StackPanel>
        </Border>
    </StackPanel>
</Border>
```

### PricingViewModel Updates

```csharp
[ObservableProperty] private bool _isLoadingActiveListings;
[ObservableProperty] private PriceLookupResult? _activeListingResult;

[RelayCommand]
private async Task GetActiveListingsAsync()
{
    if (CurrentCard == null) return;

    IsLoadingActiveListings = true;

    try
    {
        var listings = await _activeListingService.SearchListingsAsync(CurrentCard);
        ActiveListingResult = _activeListingService.CalculateMarketValue(listings, CurrentCard);

        if (ActiveListingResult.Success && ActiveListingResult.MedianPrice.HasValue)
        {
            // Optionally auto-fill market value
            // MarketValue = ActiveListingResult.MedianPrice;
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to fetch active listings");
        ActiveListingResult = new PriceLookupResult
        {
            Success = false,
            Confidence = PriceConfidence.None,
            Source = $"Error: {ex.Message}"
        };
    }
    finally
    {
        IsLoadingActiveListings = false;
    }
}

[RelayCommand]
private void OpenActiveListings()
{
    if (CurrentCard == null) return;

    var searchQuery = new TitleTemplateService().GenerateTitle(
        CurrentCard, _settingsService.Load().EbaySearchTemplate);

    var url = $"https://www.ebay.com/sch/i.html?_nkw={Uri.EscapeDataString(searchQuery)}" +
              "&_sacat=261328&LH_ItemCondition=3000"; // Category: Sports Cards, Condition: Used

    _browserService.OpenUrl(url);
}
```

## Settings Updates

### New eBay API Settings

```csharp
// AppSettings.cs
public string? EbayClientId { get; set; }
public string? EbayClientSecret { get; set; }
```

### SettingsView.axaml

```xml
<!-- eBay API Settings -->
<TextBlock Text="eBay Client ID" FontWeight="SemiBold" Margin="0,8,0,0"/>
<TextBox Text="{Binding EbayClientId}"
         Watermark="Your eBay Client ID"/>

<TextBlock Text="eBay Client Secret" FontWeight="SemiBold" Margin="0,8,0,0"/>
<TextBox Text="{Binding EbayClientSecret}"
         PasswordChar="*" Watermark="Your eBay Client Secret"/>

<TextBlock TextWrapping="Wrap" FontSize="11" Opacity="0.5" Margin="0,-6,0,0">
    Get API credentials from eBay Developer Program (free for sellers, 5000 calls/day)
</TextBlock>

<Button Content="Test eBay Connection" Command="{Binding TestEbayCommand}"
        IsEnabled="{Binding !IsTestingEbay}"/>
<TextBlock Text="{Binding EbayStatus}" FontSize="12" Opacity="0.7"/>
```

## Implementation Phases

### Phase 1: Database & Models (4-5 hours)
**Files to create/modify:**
- `CardLister/Models/ActiveListingRecord.cs` - New model
- `CardLister/Data/CardListerDbContext.cs` - Add DbSet
- EF migration for new table

**Tasks:**
1. Create `ActiveListingRecord` model
2. Add `DbSet<ActiveListingRecord>` to DbContext
3. Configure indexes
4. Generate migration

### Phase 2: eBay API Service (8-10 hours)
**Files to create:**
- `CardLister/Services/IActiveListingService.cs` - Interface
- `CardLister/Services/EbayBrowseService.cs` - Implementation
- `CardLister/Models/EbayApiModels.cs` - API response DTOs

**Tasks:**
1. Implement OAuth token management
2. Implement search by keyword
3. Implement response parsing
4. Add rate limiting (2 req/sec, 5000/day)
5. Add database caching (7-day freshness)
6. Implement price calculation

### Phase 3: Settings Integration (3-4 hours)
**Files to modify:**
- `CardLister/Models/AppSettings.cs`
- `CardLister/ViewModels/SettingsViewModel.cs`
- `CardLister/Views/SettingsView.axaml`

**Tasks:**
1. Add `EbayClientId` and `EbayClientSecret` to AppSettings
2. Add properties to SettingsViewModel
3. Add UI controls in SettingsView
4. Add connection test command

### Phase 4: Pricing UI Integration (4-5 hours)
**Files to modify:**
- `CardLister/ViewModels/PricingViewModel.cs`
- `CardLister/Views/PricingView.axaml`

**Tasks:**
1. Inject `IActiveListingService`
2. Add `GetActiveListingsCommand`
3. Add UI section in PricingView
4. Add "View All Active Listings" link

### Phase 5: Testing & Polish (4-6 hours)
**Tasks:**
1. Test with 20+ real cards
2. Verify OAuth flow works
3. Test rate limiting
4. Verify caching logic
5. Test error handling (API down, no results, auth failure)
6. Performance testing

## Total Estimated Time: 23-30 hours

## Future Enhancements

### Image Search (Phase 6)
- Implement `SearchByImageAsync()`
- Add drag-drop image upload in PricingView
- Use eBay's image-based search endpoint
- Match card photo to active listings visually

### Advanced Filtering
- Filter by auction vs Buy It Now
- Filter by shipping type (Free only)
- Filter by seller rating
- Filter by condition (graded only, raw only)

### Price Alerts
- Track specific cards
- Alert when new listings appear below target price
- Daily/weekly digest of watched cards

### Competitive Analysis
- Show price distribution chart
- Compare your price to active comps
- Suggest optimal pricing strategy

## Success Criteria

After Phase 5, the feature is ready if:
- [ ] ✅ OAuth authentication works reliably
- [ ] ✅ Can fetch active listings for 90%+ of cards
- [ ] ✅ Price calculations are reasonable (±15% of manual research)
- [ ] ✅ Rate limiting prevents API quota issues
- [ ] ✅ 7-day caching reduces redundant API calls
- [ ] ✅ UI integrates smoothly with existing pricing workflow
- [ ] ✅ Error handling is graceful (shows clear messages)
- [ ] ✅ No performance degradation in pricing workflow

## API Documentation References

- **eBay Browse API:** https://developer.ebay.com/api-docs/buy/browse/overview.html
- **OAuth Guide:** https://developer.ebay.com/api-docs/static/oauth-client-credentials-grant.html
- **Search endpoint:** https://developer.ebay.com/api-docs/buy/browse/resources/item_summary/methods/search
- **Rate limits:** https://developer.ebay.com/support/kb/article-rate-limits

## Risk Mitigation

**Risk 1: API Quota Exhaustion**
- **Probability:** Medium (5000 calls/day = ~200 cards if cached)
- **Mitigation:** 7-day cache, batch lookups, user notification at 80% quota
- **Recovery:** Graceful degradation to manual research

**Risk 2: Authentication Failures**
- **Probability:** Low-Medium (OAuth complexity)
- **Mitigation:** Clear error messages, token refresh logic, test command
- **Recovery:** Settings validation, re-authentication flow

**Risk 3: Poor Match Quality**
- **Probability:** Medium (search quality depends on query)
- **Mitigation:** Reuse existing search templates, fuzzy matching, user can view all results
- **Recovery:** Manual Terapeak workflow always available

## Comparison to Point130 Approach

| Aspect | Point130 (Shelved) | eBay Browse API |
|--------|-------------------|-----------------|
| Data Type | Sold prices (historical) | Active listings (current) |
| Legality | ToS violation (scraping) | Official API |
| Stability | Fragile (site changes) | Stable, documented |
| Cost | Free (but risky) | Free (5000 calls/day) |
| Data Quality | "Not very effective" | High quality, structured |
| Features | Keyword only | Keyword + image search |
| Rate Limits | Manual delays | Official quota management |
| Maintenance | High (site changes) | Low (stable API) |

## Notes

- This feature is **complementary** to existing Terapeak/eBay sold workflow (not a replacement)
- Active listings show "asking prices" (what sellers want), sold data shows "realized prices" (what buyers paid)
- Both data points together give a complete picture of market value
- The Browse API infrastructure can be extended later for image search and price alerts
