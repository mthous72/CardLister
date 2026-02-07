using System.Collections.Generic;

namespace CardLister.Models
{
    public class AppSettings
    {
        public string? OpenRouterApiKey { get; set; }
        public string? ImgBBApiKey { get; set; }
        public bool IsEbaySeller { get; set; }
        public string DefaultShippingProfile { get; set; } = "4 oz";
        public string DefaultCondition { get; set; } = "Near Mint";
        public decimal WhatnotFeePercent { get; set; } = 11.0m;
        public decimal EbayFeePercent { get; set; } = 13.25m;
        public decimal DefaultShippingCostPwe { get; set; } = 1.00m;
        public decimal DefaultShippingCostBmwt { get; set; } = 4.50m;
        public int PriceStalenessThresholdDays { get; set; } = 30;
        public string DefaultModel { get; set; } = "nvidia/nemotron-nano-12b-v2-vl:free";
        public bool EnableVariationVerification { get; set; } = true;
        public bool AutoApplyHighConfidenceSuggestions { get; set; } = true;
        public bool RunConfirmationPass { get; set; } = true;
        public bool EnableChecklistLearning { get; set; } = true;
        public List<string> CustomGradingCompanies { get; set; } = new();

        // Bulk Scan Concurrency Settings
        // For free models (:free suffix), use 1 to avoid rate limits with the 4-second delay
        // For paid models (with credits), use 3-4 for optimal performance
        public int MaxConcurrentScans { get; set; } = 1;
    }
}
