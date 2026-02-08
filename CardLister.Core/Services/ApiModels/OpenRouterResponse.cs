using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CardLister.Core.Services.ApiModels
{
    public class OpenRouterResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenRouterChoice> Choices { get; set; } = new();
    }

    public class OpenRouterChoice
    {
        [JsonPropertyName("message")]
        public OpenRouterResponseMessage Message { get; set; } = new();
    }

    public class OpenRouterResponseMessage
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }
}
