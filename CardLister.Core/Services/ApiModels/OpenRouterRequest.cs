using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CardLister.Core.Services.ApiModels
{
    public class OpenRouterRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OpenRouterMessage> Messages { get; set; } = new();

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 1024;
    }

    public class OpenRouterMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("content")]
        public object Content { get; set; } = string.Empty;
    }

    public class OpenRouterContentPart
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("image_url")]
        public OpenRouterImageUrl? ImageUrl { get; set; }
    }

    public class OpenRouterImageUrl
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }
}
