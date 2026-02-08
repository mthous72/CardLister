using Microsoft.AspNetCore.Mvc;
using CardLister.Core.Services;
using CardLister.Core.Models;
using CardLister.Core.Models.Enums;
using CardLister.Web.Models;

namespace CardLister.Web.Controllers
{
    public class InventoryController : Controller
    {
        private readonly ICardRepository _cardRepository;
        private readonly ILogger<InventoryController> _logger;

        public InventoryController(ICardRepository cardRepository, ILogger<InventoryController> logger)
        {
            _cardRepository = cardRepository;
            _logger = logger;
        }

        // GET: Inventory
        public async Task<IActionResult> Index(string? search, string? sport, string? status, int page = 1, int pageSize = 20)
        {
            try
            {
                var allCards = await _cardRepository.GetAllCardsAsync();

                // Apply filters
                var filtered = allCards.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    filtered = filtered.Where(c =>
                        (c.PlayerName?.ToLower().Contains(searchLower) ?? false) ||
                        (c.Brand?.ToLower().Contains(searchLower) ?? false) ||
                        (c.Team?.ToLower().Contains(searchLower) ?? false) ||
                        (c.Manufacturer?.ToLower().Contains(searchLower) ?? false));
                }

                if (!string.IsNullOrWhiteSpace(sport) && sport != "All")
                {
                    if (Enum.TryParse<Sport>(sport, out var sportEnum))
                        filtered = filtered.Where(c => c.Sport == sportEnum);
                }

                if (!string.IsNullOrWhiteSpace(status) && status != "All")
                {
                    if (Enum.TryParse<CardStatus>(status, out var statusEnum))
                        filtered = filtered.Where(c => c.Status == statusEnum);
                }

                var totalCount = filtered.Count();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                // Apply pagination
                var paginatedCards = filtered
                    .OrderByDescending(c => c.UpdatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var viewModel = new InventoryListViewModel
                {
                    Cards = paginatedCards,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalCount = totalCount,
                    PageSize = pageSize,
                    SearchQuery = search,
                    SelectedSport = sport ?? "All",
                    SelectedStatus = status ?? "All"
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading inventory");
                return View(new InventoryListViewModel());
            }
        }

        // GET: Inventory/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var card = await _cardRepository.GetCardAsync(id);
                if (card == null)
                {
                    return NotFound();
                }

                var viewModel = MapCardToDetailsViewModel(card);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading card details for ID {CardId}", id);
                return NotFound();
            }
        }

        // GET: Inventory/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var card = await _cardRepository.GetCardAsync(id);
                if (card == null)
                {
                    return NotFound();
                }

                var viewModel = MapCardToDetailsViewModel(card);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading card for edit, ID {CardId}", id);
                return NotFound();
            }
        }

        // POST: Inventory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CardDetailsViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var card = await _cardRepository.GetCardAsync(id);
                if (card == null)
                {
                    return NotFound();
                }

                // Update card properties from model
                card.PlayerName = model.PlayerName ?? string.Empty;
                card.Sport = model.Sport;
                card.Brand = model.Brand;
                card.Manufacturer = model.Manufacturer;
                card.Year = model.Year;
                card.CardNumber = model.CardNumber;
                card.Team = model.Team;
                card.SetName = model.SetName;
                card.VariationType = model.VariationType;
                card.ParallelName = model.ParallelName;
                card.SerialNumbered = model.SerialNumbered;
                card.IsShortPrint = model.IsShortPrint;
                card.IsSSP = model.IsSSP;
                card.IsRookie = model.IsRookie;
                card.IsAuto = model.IsAuto;
                card.IsRelic = model.IsRelic;
                card.Condition = model.Condition;
                card.IsGraded = model.IsGraded;
                card.GradeCompany = model.GradeCompany;
                card.GradeValue = model.GradeValue;
                card.CertNumber = model.CertNumber;
                card.AutoGrade = model.AutoGrade;
                card.CostBasis = model.CostBasis;
                card.CostSource = model.CostSource;
                card.CostDate = model.CostDate;
                card.CostNotes = model.CostNotes;
                card.Quantity = model.Quantity;
                card.EstimatedValue = model.EstimatedValue;
                card.ListingPrice = model.ListingPrice;
                card.ListingType = model.ListingType;
                card.Offerable = model.Offerable;
                card.ShippingProfile = model.ShippingProfile;
                card.WhatnotCategory = model.WhatnotCategory;
                card.WhatnotSubcategory = model.WhatnotSubcategory;
                card.Notes = model.Notes;
                card.UpdatedAt = DateTime.UtcNow;

                await _cardRepository.UpdateCardAsync(card);

                TempData["SuccessMessage"] = "Card updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating card ID {CardId}", id);
                ModelState.AddModelError("", "Failed to update card. Please try again.");
                return View(model);
            }
        }

        // POST: Inventory/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var card = await _cardRepository.GetCardAsync(id);
                if (card == null)
                {
                    return NotFound();
                }

                await _cardRepository.DeleteCardAsync(id);

                TempData["SuccessMessage"] = $"Card '{card.PlayerName}' deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting card ID {CardId}", id);
                TempData["ErrorMessage"] = "Failed to delete card. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        private CardDetailsViewModel MapCardToDetailsViewModel(Card card)
        {
            return new CardDetailsViewModel
            {
                Id = card.Id,
                PlayerName = card.PlayerName,
                Sport = card.Sport,
                Brand = card.Brand,
                Manufacturer = card.Manufacturer,
                Year = card.Year,
                CardNumber = card.CardNumber,
                Team = card.Team,
                SetName = card.SetName,
                VariationType = card.VariationType,
                ParallelName = card.ParallelName,
                SerialNumbered = card.SerialNumbered,
                IsShortPrint = card.IsShortPrint,
                IsSSP = card.IsSSP,
                IsRookie = card.IsRookie,
                IsAuto = card.IsAuto,
                IsRelic = card.IsRelic,
                Condition = card.Condition,
                IsGraded = card.IsGraded,
                GradeCompany = card.GradeCompany,
                GradeValue = card.GradeValue,
                CertNumber = card.CertNumber,
                AutoGrade = card.AutoGrade,
                CostBasis = card.CostBasis,
                CostSource = card.CostSource,
                CostDate = card.CostDate,
                CostNotes = card.CostNotes,
                Quantity = card.Quantity,
                EstimatedValue = card.EstimatedValue,
                ListingPrice = card.ListingPrice,
                ListingType = card.ListingType,
                Offerable = card.Offerable,
                ShippingProfile = card.ShippingProfile,
                WhatnotCategory = card.WhatnotCategory,
                WhatnotSubcategory = card.WhatnotSubcategory,
                Notes = card.Notes,
                Status = card.Status,
                ImagePathFront = card.ImagePathFront,
                ImagePathBack = card.ImagePathBack,
                ImageUrl1 = card.ImageUrl1,
                ImageUrl2 = card.ImageUrl2,
                CreatedAt = card.CreatedAt,
                UpdatedAt = card.UpdatedAt
            };
        }
    }
}
