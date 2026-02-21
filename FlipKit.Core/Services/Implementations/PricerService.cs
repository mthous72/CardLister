using System;
using System.Collections.Generic;
using FlipKit.Core.Helpers;
using FlipKit.Core.Models;

namespace FlipKit.Core.Services
{
    public class PricerService : IPricerService
    {
        private readonly ISettingsService _settingsService;
        private readonly TitleTemplateService _titleTemplateService;

        public PricerService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _titleTemplateService = new TitleTemplateService();
        }

        public string BuildTerapeakUrl(Card card)
        {
            // Use customizable search template for Terapeak
            var settings = _settingsService.Load();
            var searchQuery = _titleTemplateService.GenerateTitle(card, settings.TerapeakSearchTemplate);

            var query = Uri.EscapeDataString(searchQuery);
            return $"https://www.ebay.com/sh/research?marketplace=EBAY-US&keywords={query}&tabName=SOLD";
        }

        public string BuildEbaySoldUrl(Card card)
        {
            var settings = _settingsService.Load();

            // Use smart query builder if enabled
            if (settings.UseSmartEbayQuery)
            {
                return BuildSmartEbayUrl(card);
            }
            else
            {
                // Fall back to template-based approach
                var searchQuery = _titleTemplateService.GenerateTitle(card, settings.EbaySearchTemplate);
                var query = Uri.EscapeDataString(searchQuery);
                return $"https://www.ebay.com/sch/i.html?_nkw={query}&_sacat=261328&LH_Sold=1&LH_Complete=1";
            }
        }

        public string BuildSmartEbayQuery(Card card)
        {
            var queryParts = new List<string>();

            // === ALWAYS INCLUDE (Core Identifiers) ===

            if (card.Year.HasValue)
                queryParts.Add(card.Year.Value.ToString());

            if (!string.IsNullOrWhiteSpace(card.PlayerName))
                queryParts.Add(card.PlayerName);

            // === CONDITIONALLY INCLUDE (Smart Logic) ===

            // Sport: Include if set (helps filter category)
            if (card.Sport.HasValue)
                queryParts.Add(card.Sport.Value.ToString());

            // Brand: Always include if available
            if (!string.IsNullOrWhiteSpace(card.Brand))
                queryParts.Add(card.Brand);

            // SetName: Include ONLY if different from Brand (avoid redundancy)
            if (!string.IsNullOrWhiteSpace(card.SetName) &&
                !string.Equals(card.SetName, card.Brand, StringComparison.OrdinalIgnoreCase))
            {
                queryParts.Add(card.SetName);
            }

            // Team: Include if available (helps filter)
            if (!string.IsNullOrWhiteSpace(card.Team))
                queryParts.Add(card.Team);

            // ParallelName: Include if available and meaningful
            if (!string.IsNullOrWhiteSpace(card.ParallelName) &&
                !card.ParallelName.Equals("Base", StringComparison.OrdinalIgnoreCase))
            {
                queryParts.Add(card.ParallelName);
            }

            // VariationType: Include ONLY if it's meaningful (not "Base" or generic)
            if (!string.IsNullOrWhiteSpace(card.VariationType) &&
                !card.VariationType.Equals("Base", StringComparison.OrdinalIgnoreCase) &&
                !card.VariationType.Equals("Standard", StringComparison.OrdinalIgnoreCase))
            {
                queryParts.Add(card.VariationType);
            }

            // === ATTRIBUTES (High-Value Features) ===

            var attributes = BuildAttributes(card);
            if (!string.IsNullOrWhiteSpace(attributes))
                queryParts.Add(attributes);

            // === GRADING INFO ===

            if (card.IsGraded)
            {
                var gradeInfo = BuildGrade(card);
                if (!string.IsNullOrWhiteSpace(gradeInfo))
                    queryParts.Add(gradeInfo);
            }

            // === INTENTIONALLY EXCLUDED ===
            // - CardNumber: Too specific, drastically reduces results
            // - SerialNumbered (/99, /10): Too specific for comp research
            // - Manufacturer: Redundant with Brand
            // - CertNumber: Too specific
            // - Condition: Not searchable on eBay

            return string.Join(" ", queryParts);
        }

        public string BuildSmartEbayUrl(Card card)
        {
            var query = BuildSmartEbayQuery(card);
            var escapedQuery = Uri.EscapeDataString(query);
            return $"https://www.ebay.com/sch/i.html?_nkw={escapedQuery}&_sacat=261328&LH_Sold=1&LH_Complete=1";
        }

        private string BuildAttributes(Card card)
        {
            var attrs = new List<string>();
            if (card.IsRookie) attrs.Add("RC");
            if (card.IsAuto) attrs.Add("Auto");
            if (card.IsRelic) attrs.Add("Relic");
            if (card.IsShortPrint) attrs.Add("SP");
            if (card.IsSSP) attrs.Add("SSP");
            return string.Join(" ", attrs);
        }

        private string BuildGrade(Card card)
        {
            if (!card.IsGraded) return string.Empty;

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(card.GradeCompany))
                parts.Add(card.GradeCompany);
            if (!string.IsNullOrWhiteSpace(card.GradeValue))
                parts.Add(card.GradeValue);

            return string.Join(" ", parts);
        }

        public decimal SuggestPrice(decimal estimatedValue, Card card)
        {
            var price = estimatedValue;

            var variation = (card.VariationType ?? "Base").ToLower();

            if (variation == "base")
            {
                price *= 0.80m;
            }
            else if (!string.IsNullOrEmpty(card.SerialNumbered))
            {
                var serial = card.SerialNumbered.Replace("/", "");
                if (int.TryParse(serial, out var num))
                {
                    price *= num <= 10 ? 0.95m : num <= 25 ? 0.92m : 0.88m;
                }
                else
                {
                    price *= 0.88m;
                }
            }
            else
            {
                price *= 0.85m;
            }

            // Boost for special attributes
            if (card.IsRookie) price *= 1.05m;
            if (card.IsAuto) price *= 1.02m;

            // Round to nice price points
            if (price >= 100)
                price = Math.Round(price / 5) * 5;
            else if (price >= 20)
                price = Math.Round(price);
            else if (price >= 5)
                price = Math.Round(price * 2) / 2;
            else
                price = Math.Round(price, 2);

            return Math.Max(price, 0.99m);
        }

        public decimal CalculateNet(decimal salePrice, decimal feePercent = 11m)
        {
            return PriceCalculator.CalculateNet(salePrice, feePercent);
        }
    }
}
