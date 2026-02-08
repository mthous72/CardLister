using Microsoft.AspNetCore.Mvc;
using CardLister.Core.Services;
using CardLister.Core.Models.Enums;
using CardLister.Web.Models;

namespace CardLister.Web.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ICardRepository _cardRepository;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            ICardRepository cardRepository,
            ILogger<ReportsController> logger)
        {
            _cardRepository = cardRepository;
            _logger = logger;
        }

        // GET: Reports
        public async Task<IActionResult> Index()
        {
            try
            {
                var allCards = await _cardRepository.GetAllCardsAsync();

                var viewModel = new ReportsViewModel
                {
                    // Inventory Stats
                    TotalCards = allCards.Count,
                    DraftCards = allCards.Count(c => c.Status == CardStatus.Draft),
                    PricedCards = allCards.Count(c => c.Status == CardStatus.Priced),
                    ReadyCards = allCards.Count(c => c.Status == CardStatus.Ready),
                    ListedCards = allCards.Count(c => c.Status == CardStatus.Listed),
                    SoldCards = allCards.Count(c => c.Status == CardStatus.Sold),

                    // Financial Stats
                    TotalInventoryValue = allCards
                        .Where(c => c.ListingPrice.HasValue && c.Status != CardStatus.Sold)
                        .Sum(c => c.ListingPrice!.Value),
                    TotalCostBasis = allCards
                        .Where(c => c.CostBasis.HasValue && c.Status != CardStatus.Sold)
                        .Sum(c => c.CostBasis!.Value),
                    TotalRevenue = allCards
                        .Where(c => c.SalePrice.HasValue)
                        .Sum(c => c.SalePrice!.Value),
                    TotalProfit = allCards
                        .Where(c => c.SalePrice.HasValue && c.CostBasis.HasValue)
                        .Sum(c => c.SalePrice!.Value - c.CostBasis!.Value),

                    // Sport Breakdown
                    CardsBySport = allCards
                        .GroupBy(c => c.Sport)
                        .ToDictionary(g => g.Key.ToString(), g => g.Count()),

                    // Recent Sales (last 30 days)
                    RecentSales = allCards
                        .Where(c => c.Status == CardStatus.Sold && c.SaleDate.HasValue)
                        .Where(c => c.SaleDate!.Value >= DateTime.UtcNow.AddDays(-30))
                        .OrderByDescending(c => c.SaleDate)
                        .Take(10)
                        .ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reports");
                return View(new ReportsViewModel());
            }
        }

        // GET: Reports/Sales
        public async Task<IActionResult> Sales(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                // Default to last 30 days if no dates provided
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                var allCards = await _cardRepository.GetAllCardsAsync();
                var soldCards = allCards
                    .Where(c => c.Status == CardStatus.Sold && c.SaleDate.HasValue)
                    .Where(c => c.SaleDate!.Value >= start && c.SaleDate!.Value <= end)
                    .OrderByDescending(c => c.SaleDate)
                    .ToList();

                var viewModel = new SalesReportViewModel
                {
                    StartDate = start,
                    EndDate = end,
                    SoldCards = soldCards,
                    TotalSales = soldCards.Count,
                    TotalRevenue = soldCards.Where(c => c.SalePrice.HasValue).Sum(c => c.SalePrice!.Value),
                    TotalProfit = soldCards
                        .Where(c => c.SalePrice.HasValue && c.CostBasis.HasValue)
                        .Sum(c => c.SalePrice!.Value - c.CostBasis!.Value),
                    AverageProfit = soldCards.Any()
                        ? soldCards
                            .Where(c => c.SalePrice.HasValue && c.CostBasis.HasValue)
                            .Average(c => c.SalePrice!.Value - c.CostBasis!.Value)
                        : 0
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sales report");
                return View(new SalesReportViewModel
                {
                    StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
                    EndDate = endDate ?? DateTime.UtcNow
                });
            }
        }

        // GET: Reports/Financial
        public async Task<IActionResult> Financial()
        {
            try
            {
                var allCards = await _cardRepository.GetAllCardsAsync();

                // Calculate profitability by sport
                var profitBySport = allCards
                    .Where(c => c.Status == CardStatus.Sold && c.SalePrice.HasValue && c.CostBasis.HasValue)
                    .GroupBy(c => c.Sport)
                    .Select(g => new SportProfitability
                    {
                        Sport = g.Key.ToString(),
                        TotalSales = g.Count(),
                        TotalRevenue = g.Sum(c => c.SalePrice!.Value),
                        TotalCost = g.Sum(c => c.CostBasis!.Value),
                        TotalProfit = g.Sum(c => c.SalePrice!.Value - c.CostBasis!.Value),
                        AverageProfit = g.Average(c => c.SalePrice!.Value - c.CostBasis!.Value),
                        ProfitMargin = g.Sum(c => c.CostBasis!.Value) > 0
                            ? (g.Sum(c => c.SalePrice!.Value - c.CostBasis!.Value) / g.Sum(c => c.CostBasis!.Value)) * 100
                            : 0
                    })
                    .OrderByDescending(s => s.TotalProfit)
                    .ToList();

                // Calculate inventory turnover
                var activeInventory = allCards
                    .Where(c => c.Status != CardStatus.Sold)
                    .ToList();

                var soldInventory = allCards
                    .Where(c => c.Status == CardStatus.Sold)
                    .ToList();

                var viewModel = new FinancialReportViewModel
                {
                    // Overall Stats
                    TotalInventoryValue = activeInventory
                        .Where(c => c.ListingPrice.HasValue)
                        .Sum(c => c.ListingPrice!.Value),
                    TotalInventoryCost = activeInventory
                        .Where(c => c.CostBasis.HasValue)
                        .Sum(c => c.CostBasis!.Value),
                    TotalRevenue = soldInventory
                        .Where(c => c.SalePrice.HasValue)
                        .Sum(c => c.SalePrice!.Value),
                    TotalProfit = soldInventory
                        .Where(c => c.SalePrice.HasValue && c.CostBasis.HasValue)
                        .Sum(c => c.SalePrice!.Value - c.CostBasis!.Value),
                    OverallProfitMargin = soldInventory.Where(c => c.CostBasis.HasValue).Sum(c => c.CostBasis!.Value) > 0
                        ? (soldInventory
                            .Where(c => c.SalePrice.HasValue && c.CostBasis.HasValue)
                            .Sum(c => c.SalePrice!.Value - c.CostBasis!.Value)
                            / soldInventory.Where(c => c.CostBasis.HasValue).Sum(c => c.CostBasis!.Value)) * 100
                        : 0,

                    // Inventory Metrics
                    ActiveCards = activeInventory.Count,
                    SoldCards = soldInventory.Count,
                    InventoryTurnoverRate = (activeInventory.Count + soldInventory.Count) > 0
                        ? (decimal)soldInventory.Count / (activeInventory.Count + soldInventory.Count) * 100
                        : 0,

                    // Breakdown by Sport
                    ProfitBySport = profitBySport
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading financial report");
                return View(new FinancialReportViewModel());
            }
        }
    }
}
