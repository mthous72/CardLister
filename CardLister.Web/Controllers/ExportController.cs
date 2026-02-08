using Microsoft.AspNetCore.Mvc;
using CardLister.Core.Services;
using CardLister.Core.Models;
using CardLister.Core.Models.Enums;
using CardLister.Web.Models;
using System.Text;

namespace CardLister.Web.Controllers
{
    public class ExportController : Controller
    {
        private readonly ICardRepository _cardRepository;
        private readonly IExportService _exportService;
        private readonly ILogger<ExportController> _logger;
        private readonly IWebHostEnvironment _environment;

        public ExportController(
            ICardRepository cardRepository,
            IExportService exportService,
            ILogger<ExportController> logger,
            IWebHostEnvironment environment)
        {
            _cardRepository = cardRepository;
            _exportService = exportService;
            _logger = logger;
            _environment = environment;
        }

        // GET: Export
        public async Task<IActionResult> Index()
        {
            try
            {
                var allCards = await _cardRepository.GetAllCardsAsync();

                // Get cards ready for export (status = Ready)
                var readyCards = allCards.Where(c => c.Status == CardStatus.Ready).ToList();

                // Get cards that could be exported (have listing price)
                var pricedCards = allCards
                    .Where(c => c.Status == CardStatus.Priced && c.ListingPrice.HasValue)
                    .ToList();

                var viewModel = new ExportListViewModel
                {
                    ReadyCards = readyCards,
                    PricedCards = pricedCards
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading export list");
                return View(new ExportListViewModel());
            }
        }

        // POST: Export/MarkAsReady
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsReady(int cardId)
        {
            try
            {
                var card = await _cardRepository.GetCardAsync(cardId);
                if (card == null)
                {
                    return NotFound();
                }

                card.Status = CardStatus.Ready;
                card.UpdatedAt = DateTime.UtcNow;
                await _cardRepository.UpdateCardAsync(card);

                TempData["SuccessMessage"] = $"'{card.PlayerName}' marked as ready for export!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking card as ready");
                TempData["ErrorMessage"] = $"Failed to mark card as ready: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Export/GenerateCsv
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateCsv(string platform = "Whatnot")
        {
            try
            {
                var allCards = await _cardRepository.GetAllCardsAsync();
                var readyCards = allCards.Where(c => c.Status == CardStatus.Ready).ToList();

                if (!readyCards.Any())
                {
                    TempData["ErrorMessage"] = "No cards are ready for export. Mark cards as 'Ready' first.";
                    return RedirectToAction(nameof(Index));
                }

                // Validate all cards
                var validationErrors = new List<string>();
                foreach (var card in readyCards)
                {
                    var errors = _exportService.ValidateCardForExport(card);
                    if (errors.Any())
                    {
                        validationErrors.Add($"{card.PlayerName}: {string.Join(", ", errors)}");
                    }
                }

                if (validationErrors.Any())
                {
                    TempData["ErrorMessage"] = $"Validation errors: {string.Join("; ", validationErrors.Take(5))}";
                    return RedirectToAction(nameof(Index));
                }

                // Generate CSV in memory
                var exportPlatform = Enum.TryParse<ExportPlatform>(platform, out var p) ? p : ExportPlatform.Whatnot;
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
                var fileName = $"{platform.ToLower()}-export-{timestamp}.csv";

                // Create temp file
                var tempPath = Path.Combine(Path.GetTempPath(), fileName);
                await _exportService.ExportCsvAsync(readyCards, tempPath, exportPlatform);

                // Read file into memory
                var fileBytes = await System.IO.File.ReadAllBytesAsync(tempPath);

                // Delete temp file
                System.IO.File.Delete(tempPath);

                _logger.LogInformation("Generated CSV export: {FileName} with {Count} cards", fileName, readyCards.Count);

                // Return file for download
                return File(fileBytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating CSV export");
                TempData["ErrorMessage"] = $"Failed to generate CSV: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Export/Preview/5
        public async Task<IActionResult> Preview(int id)
        {
            try
            {
                var card = await _cardRepository.GetCardAsync(id);
                if (card == null)
                {
                    return NotFound();
                }

                var viewModel = new ExportPreviewViewModel
                {
                    Card = card,
                    GeneratedTitle = _exportService.GenerateTitle(card),
                    GeneratedDescription = _exportService.GenerateDescription(card),
                    ValidationErrors = _exportService.ValidateCardForExport(card)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading export preview for card {CardId}", id);
                return NotFound();
            }
        }

        // POST: Export/ValidateCard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidateCard(int cardId)
        {
            try
            {
                var card = await _cardRepository.GetCardAsync(cardId);
                if (card == null)
                {
                    return Json(new { success = false, errors = new[] { "Card not found" } });
                }

                var errors = _exportService.ValidateCardForExport(card);
                return Json(new { success = errors.Count == 0, errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating card {CardId}", cardId);
                return Json(new { success = false, errors = new[] { ex.Message } });
            }
        }
    }
}
