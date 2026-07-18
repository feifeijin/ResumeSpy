using System.Text.Json.Serialization;

namespace ResumeSpy.Core.AI
{
    /// <summary>
    /// Generic DTOs for OpenAI-compatible chat completion APIs
    /// Compatible with OpenAI, Azure OpenAI, Hugging Face, and other providers
    /// </summary>
    public class GenericChatCompletionResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        
        [JsonPropertyName("object")]
        public string? Object { get; set; }
        
        [JsonPropertyName("created")]
        public long Created { get; set; }
        
        [JsonPropertyName("model")]
        public string? Model { get; set; }
        
        [JsonPropertyName("choices")]
        public GenericChatChoice[]? Choices { get; set; }
        
        [JsonPropertyName("usage")]
        public GenericChatUsage? Usage { get; set; }
        
        [JsonPropertyName("system_fingerprint")]
        public string? SystemFingerprint { get; set; }
    }

    public class GenericChatChoice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }
        
        [JsonPropertyName("message")]
        public GenericChatMessage? Message { get; set; }
        
        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
        
        [JsonPropertyName("content_filter_results")]
        public GenericContentFilterResults? ContentFilterResults { get; set; }
    }

    public class GenericChatMessage
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }
        
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    public class GenericChatUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }
        
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }
        
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
        
        [JsonPropertyName("prompt_tokens_details")]
        public object? PromptTokensDetails { get; set; }
        
        [JsonPropertyName("completion_tokens_details")]
        public object? CompletionTokensDetails { get; set; }
    }

    public class GenericContentFilterResults
    {
        [JsonPropertyName("hate")]
        public GenericFilterResult? Hate { get; set; }
        
        [JsonPropertyName("self_harm")]
        public GenericFilterResult? SelfHarm { get; set; }
        
        [JsonPropertyName("sexual")]
        public GenericFilterResult? Sexual { get; set; }
        
        [JsonPropertyName("violence")]
        public GenericFilterResult? Violence { get; set; }
        
        [JsonPropertyName("jailbreak")]
        public GenericFilterResult? Jailbreak { get; set; }
        
        [JsonPropertyName("profanity")]
        public GenericFilterResult? Profanity { get; set; }
    }

    public class GenericFilterResult
    {
        [JsonPropertyName("filtered")]
        public bool Filtered { get; set; }
        
        [JsonPropertyName("detected")]
        public bool Detected { get; set; }
    }

    /// <summary>
    /// Request payload for chat completion APIs
    /// </summary>
    public class GenericChatCompletionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;
        
        [JsonPropertyName("messages")]
        public GenericChatMessage[] Messages { get; set; } = Array.Empty<GenericChatMessage>();
        
        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 1000;
        
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;
        
        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;
    }
}