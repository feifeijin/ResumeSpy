using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ResumeSpy.Core.AI;
using ResumeSpy.Core.Interfaces.AI;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ResumeSpy.Infrastructure.Services.AI
{
    /// <summary>
    /// Hugging Face Inference API adapter for generative text services
    /// Provides free AI text generation with generous limits
    /// </summary>
    public class HuggingFaceTextService : IGenerativeTextService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HuggingFaceTextService> _logger;
        private readonly string _apiToken;
        private readonly string _defaultModel;
        private readonly string _endpoint;

        public HuggingFaceTextService(HttpClient httpClient, IConfiguration configuration, ILogger<HuggingFaceTextService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _apiToken = configuration["AI:HuggingFace:ApiToken"] 
                ?? throw new InvalidOperationException("HuggingFace API token not configured");
            
            _defaultModel = configuration["AI:HuggingFace:DefaultModel"] ?? "microsoft/DialoGPT-medium";
            _endpoint = configuration["AI:HuggingFace:Endpoint"] ?? "https://api-inference.huggingface.co/models";

            // Set authorization header
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiToken);
        }

        public async Task<AIResponse> GenerateResponseAsync(AIRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var modelToUse = request.ModelId ?? _defaultModel;

            try
            {
                // Combine system message and prompt
                var prompt = !string.IsNullOrWhiteSpace(request.SystemMessage) 
                    ? $"System: {request.SystemMessage}\n\nUser: {request.Prompt}\n\nAssistant:"
                    : request.Prompt;

                // For text generation models, we use a simple input format
                var payload = new
                {
                    inputs = prompt,
                    parameters = new
                    {
                        max_new_tokens = request.MaxTokens,
                        temperature = request.Temperature,
                        return_full_text = false,
                        do_sample = true
                    },
                    options = new
                    {
                        wait_for_model = true
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_endpoint}/{modelToUse}", content);
                
                // Handle rate limiting
                if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                    response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds ?? 20;
                    _logger.LogWarning("HuggingFace rate limited, waiting {Seconds} seconds", retryAfter);
                    await Task.Delay(TimeSpan.FromSeconds(Math.Min(retryAfter, 60))); // Cap at 60 seconds
                    
                    response = await _httpClient.PostAsync($"{_endpoint}/{modelToUse}", content);
                }

                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Parse response - HF returns array of results
                var results = JsonSerializer.Deserialize<HuggingFaceResponse[]>(responseContent);
                var generatedText = results?[0]?.GeneratedText ?? "";

                stopwatch.Stop();

                // Estimate token usage (rough approximation)
                var promptTokens = EstimateTokenCount(prompt);
                var completionTokens = EstimateTokenCount(generatedText);

                _logger.LogInformation(
                    "HuggingFace request completed. Model: {Model}, Estimated Tokens: {TotalTokens} ({InputTokens} in + {OutputTokens} out), Cost: Free, Latency: {Latency}ms",
                    modelToUse, promptTokens + completionTokens, promptTokens, completionTokens, stopwatch.ElapsedMilliseconds);

                return new AIResponse
                {
                    Content = generatedText.Trim(),
                    IsSuccess = true,
                    Cost = 0, // Free!
                    Latency = stopwatch.Elapsed,
                    PromptTokens = promptTokens,
                    CompletionTokens = completionTokens,
                    ProviderName = "HuggingFace",
                    ModelUsed = modelToUse
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "HuggingFace request failed. Model: {Model}, Latency: {Latency}ms", modelToUse, stopwatch.ElapsedMilliseconds);

                return new AIResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Latency = stopwatch.Elapsed,
                    ProviderName = "HuggingFace",
                    ModelUsed = modelToUse
                };
            }
        }

        private static int EstimateTokenCount(string text)
        {
            // Rough estimation: ~4 characters per token for English text
            return Math.Max(1, text.Length / 4);
        }

        private class HuggingFaceResponse
        {
            public string GeneratedText { get; set; } = string.Empty;
        }
    }
}