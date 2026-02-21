using FlipKit.Core.Models;

namespace FlipKit.Web.Models
{
    /// <summary>
    /// View model for researching comps on scanned cards before saving to database.
    /// </summary>
    public class ScanResearchViewModel
    {
        public Card ScannedCard { get; set; } = new();
        public string? FrontImagePath { get; set; }
        public string? BackImagePath { get; set; }
        public string TerapeakUrl { get; set; } = string.Empty;
        public string EbaySoldUrl { get; set; } = string.Empty;
        public decimal? EstimatedValue { get; set; }
        public decimal? ListingPrice { get; set; }
        public decimal? SuggestedPrice { get; set; }
        public string ScanMode { get; set; } = "buying";
    }
}
