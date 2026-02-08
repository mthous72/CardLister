using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CardLister.Web.Models;
using CardLister.Core.Services;
using CardLister.Core.Models.Enums;

namespace CardLister.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ICardRepository _cardRepository;

    public HomeController(ILogger<HomeController> logger, ICardRepository cardRepository)
    {
        _logger = logger;
        _cardRepository = cardRepository;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var allCards = await _cardRepository.GetAllCardsAsync();
            var dashboard = new DashboardViewModel
            {
                TotalCards = allCards.Count,
                DraftCards = allCards.Count(c => c.Status == CardStatus.Draft),
                PricedCards = allCards.Count(c => c.Status == CardStatus.Priced),
                ReadyCards = allCards.Count(c => c.Status == CardStatus.Ready),
                ListedCards = allCards.Count(c => c.Status == CardStatus.Listed),
                SoldCards = allCards.Count(c => c.Status == CardStatus.Sold),
                TotalValue = allCards.Where(c => c.ListingPrice.HasValue).Sum(c => c.ListingPrice!.Value),
                TotalRevenue = allCards.Where(c => c.SalePrice.HasValue).Sum(c => c.SalePrice!.Value)
            };
            return View(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard");
            return View(new DashboardViewModel());
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
