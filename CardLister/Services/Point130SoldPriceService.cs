using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CardLister.Data;
using CardLister.Helpers;
using CardLister.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CardLister.Services;

/// <summary>
/// Implementation of ISoldPriceService that scrapes 130point.com for eBay sold listings.
/// </summary>
public class Point130SoldPriceService : ISoldPriceService
{
    private readonly HttpClient _httpClient;
    private readonly CardListerDbContext _dbContext;
    private readonly ILogger<Point130SoldPriceService> _logger;

    // Rate limiting: Max 1 request per 10 seconds
    private static DateTime _lastRequestTime = DateTime.MinValue;
    private static readonly SemaphoreSlim _rateLimiter = new(1, 1);

    public Point130SoldPriceService(
        HttpClient httpClient,
        CardListerDbContext dbContext,
        ILogger<Point130SoldPriceService> logger)
    {
        _httpClient = httpClient;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Find matching sold price records in local database using fuzzy matching.
    /// </summary>
    public async Task<List<SoldPriceRecord>> FindMatchingRecordsAsync(Card card)
    {
        // Query database for cards in same sport and year
        var query = await _dbContext.SoldPriceRecords
            .Where(r => r.Sport == card.Sport.ToString())
            .Where(r => r.Year == card.Year)
            .ToListAsync();

        // Fuzzy match on player name (threshold 0.85)
        query = query.Where(r =>
            FuzzyMatcher.Match(r.PlayerName ?? "", card.PlayerName ?? "") >= 0.85)
            .ToList();

        // Fuzzy match on brand (threshold 0.80)
        if (!string.IsNullOrEmpty(card.Brand))
        {
            query = query.Where(r =>
                string.IsNullOrEmpty(r.Brand) ||
                FuzzyMatcher.Match(r.Brand, card.Brand) >= 0.80)
                .ToList();
        }

        // Fuzzy match on parallel (threshold 0.70)
        if (!string.IsNullOrEmpty(card.ParallelName))
        {
            query = query.Where(r =>
                string.IsNullOrEmpty(r.ParallelName) ||
                FuzzyMatcher.Match(r.ParallelName, card.ParallelName) >= 0.70)
                .ToList();
        }

        // Match graded vs raw
        if (card.IsGraded)
        {
            query = query.Where(r => r.IsGraded &&
                                     r.GradeCompany == card.GradeCompany &&
                                     r.GradeValue == card.GradeValue)
                .ToList();
        }
        else
        {
            query = query.Where(r => !r.IsGraded).ToList();
        }

        // Return most recent first
        return query.OrderByDescending(r => r.SoldDate).ToList();
    }

    /// <summary>
    /// Scrape 130point.com for sold listings (STUB - will implement in Phase 3).
    /// </summary>
    public async Task<ScrapedResult> ScrapeSoldPricesAsync(Card card, int maxResults = 20)
    {
        _logger.LogInformation("ScrapeSoldPricesAsync called for {Player} - NOT YET IMPLEMENTED", card.PlayerName);

        // Phase 3 will implement actual scraping here
        await Task.Delay(100); // Placeholder async operation

        return new ScrapedResult
        {
            Success = false,
            RecordsFound = 0,
            ErrorMessage = "Scraping not yet implemented (Phase 3)"
        };
    }

    /// <summary>
    /// Calculate market value from sold price records using statistical analysis.
    /// </summary>
    public PriceLookupResult CalculateMarketValue(List<SoldPriceRecord> records, Card card)
    {
        if (!records.Any())
        {
            return new PriceLookupResult
            {
                Success = false,
                Confidence = PriceConfidence.None,
                Source = "130point (no matches)"
            };
        }

        // Convert to double for statistical calculations
        var prices = records.Select(r => (double)r.SoldPrice).ToList();

        // Remove outliers (prices more than 2 standard deviations from mean)
        var mean = prices.Average();
        var stdDev = Math.Sqrt(prices.Average(p => Math.Pow(p - mean, 2)));
        var filtered = prices.Where(p => Math.Abs(p - mean) <= 2 * stdDev).ToList();

        if (!filtered.Any())
        {
            // All data was outliers, use original
            filtered = prices;
        }

        // Calculate statistics
        var sortedPrices = filtered.OrderBy(p => p).ToList();
        var median = sortedPrices.Count % 2 == 0
            ? (sortedPrices[sortedPrices.Count / 2 - 1] + sortedPrices[sortedPrices.Count / 2]) / 2
            : sortedPrices[sortedPrices.Count / 2];

        var average = filtered.Average();
        var low = filtered.Min();
        var high = filtered.Max();
        var mostRecent = records.Max(r => r.SoldDate);

        // Determine confidence based on sample size and data freshness
        var daysOld = (DateTime.UtcNow - mostRecent).TotalDays;
        var confidence = filtered.Count >= 5 && daysOld <= 30 ? PriceConfidence.High :
                         filtered.Count >= 2 && daysOld <= 60 ? PriceConfidence.Medium :
                         PriceConfidence.Low;

        _logger.LogInformation(
            "Calculated market value for {Player}: Median=${Median:F2}, {Count} sales, {Confidence} confidence",
            card.PlayerName, median, filtered.Count, confidence);

        return new PriceLookupResult
        {
            Success = true,
            MedianPrice = (decimal)median,
            AveragePrice = (decimal)average,
            LowPrice = (decimal)low,
            HighPrice = (decimal)high,
            SampleSize = filtered.Count,
            MostRecentSale = mostRecent,
            Confidence = confidence,
            Source = $"130point ({filtered.Count} sales)"
        };
    }

    /// <summary>
    /// Check if recent sold price data exists in local database.
    /// </summary>
    public async Task<bool> HasRecentDataAsync(Card card, int daysOld = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);

        var hasRecent = await _dbContext.SoldPriceRecords
            .Where(r => r.Sport == card.Sport.ToString())
            .Where(r => r.Year == card.Year)
            .Where(r => r.PlayerName == card.PlayerName)
            .Where(r => r.SoldDate >= cutoffDate)
            .AnyAsync();

        _logger.LogDebug(
            "HasRecentDataAsync for {Player}: {HasData} (within {Days} days)",
            card.PlayerName, hasRecent, daysOld);

        return hasRecent;
    }
}
