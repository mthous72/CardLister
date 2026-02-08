namespace CardLister.Web.Models
{
    /// <summary>
    /// View model for the scan upload page.
    /// </summary>
    public class ScanUploadViewModel
    {
        public string SelectedModel { get; set; } = "openai/gpt-4o-mini";

        public List<string> AvailableModels { get; set; } = new()
        {
            "openai/gpt-4o-mini",
            "openai/gpt-4o",
            "google/gemini-2.0-flash-exp:free",
            "google/gemini-flash-1.5",
            "anthropic/claude-3.5-sonnet",
            "meta-llama/llama-3.2-90b-vision-instruct:free",
            "qwen/qwen-2-vl-7b-instruct:free",
            "mistralai/pixtral-12b:free"
        };
    }
}
