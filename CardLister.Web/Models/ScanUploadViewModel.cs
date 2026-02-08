using CardLister.Core.Services;

namespace CardLister.Web.Models
{
    /// <summary>
    /// View model for the scan upload page.
    /// </summary>
    public class ScanUploadViewModel
    {
        public string SelectedModel { get; set; } = "nvidia/nemotron-nano-12b-v2-vl:free";

        public List<string> AvailableModels { get; set; } = new()
        {
            // Free models first (recommended)
            "nvidia/nemotron-nano-12b-v2-vl:free",
            "qwen/qwen2.5-vl-72b-instruct:free",
            "qwen/qwen2.5-vl-32b-instruct:free",
            "meta-llama/llama-4-maverick:free",
            "meta-llama/llama-4-scout:free",
            "google/gemma-3-27b-it:free",
            "mistralai/mistral-small-3.1-24b-instruct:free",
            "moonshotai/kimi-vl-a3b-thinking:free",
            "meta-llama/llama-3.2-11b-vision-instruct:free",
            "google/gemma-3-12b-it:free",
            "google/gemma-3-4b-it:free",

            // Paid models (ordered by price, cheapest to most expensive)
            "openai/gpt-4o-mini",           // $0.15/$0.60 per 1M tokens
            "google/gemini-flash-1.5",      // Low cost
            "google/gemini-2.0-flash-exp",  // Mid cost
            "openai/gpt-4o",                // Higher cost
            "anthropic/claude-3.5-sonnet"   // Premium ($5/$25 per 1M tokens)
        };
    }
}
