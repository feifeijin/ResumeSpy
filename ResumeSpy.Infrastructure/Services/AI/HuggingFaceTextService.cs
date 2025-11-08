using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ResumeSpy.Core.AI;
using ResumeSpy.Core.Interfaces.AI;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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
            
                        _defaultModel = configuration["AI:HuggingFace:DefaultModel"] ?? "meta-llama/Llama-3.1-8B-Instruct:novita";
            _endpoint = configuration["AI:HuggingFace:Endpoint"] ?? "https://router.huggingface.co/v1/chat/completions";

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
                // Create OpenAI-compatible chat completion payload
                var payload = new GenericChatCompletionRequest
                {
                    Model = modelToUse,
                    Messages = CreateMessages(request),
                    MaxTokens = request.MaxTokens,
                    Temperature = request.Temperature,
                    Stream = false
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);

                var response = await _httpClient.PostAsync(_endpoint, content);
                
                // Handle rate limiting and model loading
                if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                    response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds ?? 20;
                    _logger.LogWarning("HuggingFace rate limited or model loading, waiting {Seconds} seconds", retryAfter);
                    await Task.Delay(TimeSpan.FromSeconds(Math.Min(retryAfter, 60))); // Cap at 60 seconds
                    
                    response = await _httpClient.PostAsync(_endpoint, content);
                }

                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var chatCompletion = JsonSerializer.Deserialize<GenericChatCompletionResponse>(responseContent);
                
                if (chatCompletion?.Choices == null || chatCompletion.Choices.Length == 0)
                {
                    throw new InvalidOperationException("No choices returned from HuggingFace API");
                }

                var firstChoice = chatCompletion.Choices[0];
                var generatedText = firstChoice?.Message?.Content ?? "";

                // Check for content filtering
                if (firstChoice?.ContentFilterResults != null)
                {
                    var filtered = IsContentFiltered(firstChoice.ContentFilterResults);
                    if (filtered.IsFiltered)
                    {
                        _logger.LogWarning("Content was filtered by HuggingFace: {FilterReason}", filtered.Reason);
                    }
                }

                // Log additional API response details
                _logger.LogDebug("HuggingFace API Response - ID: {Id}, Model: {Model}, FinishReason: {FinishReason}", 
                    chatCompletion.Id, chatCompletion.Model, firstChoice?.FinishReason);

                stopwatch.Stop();

                // Get token usage from response
                var promptTokens = chatCompletion?.Usage?.PromptTokens ?? EstimateTokenCount(GetPromptText(request));
                var completionTokens = chatCompletion?.Usage?.CompletionTokens ?? EstimateTokenCount(generatedText);

                _logger.LogInformation(
                    "HuggingFace request completed. Model: {Model}, Tokens: {TotalTokens} ({InputTokens} in + {OutputTokens} out), Cost: Free, Latency: {Latency}ms",
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

        private GenericChatMessage[] CreateMessages(AIRequest request)
        {
            var messages = new List<GenericChatMessage>();

            // Add system message if provided
            if (!string.IsNullOrWhiteSpace(request.SystemMessage))
            {
                messages.Add(new GenericChatMessage { Role = "system", Content = request.SystemMessage });
            }

            // Add user message
            messages.Add(new GenericChatMessage { Role = "user", Content = request.Prompt });

            return messages.ToArray();
        }

        private string GetPromptText(AIRequest request)
        {
            return !string.IsNullOrWhiteSpace(request.SystemMessage) 
                ? $"System: {request.SystemMessage}\n\nUser: {request.Prompt}"
                : request.Prompt;
        }

        private (bool IsFiltered, string Reason) IsContentFiltered(GenericContentFilterResults filterResults)
        {
            var reasons = new List<string>();

            if (filterResults.Hate?.Filtered == true) reasons.Add("Hate");
            if (filterResults.SelfHarm?.Filtered == true) reasons.Add("Self-harm");
            if (filterResults.Sexual?.Filtered == true) reasons.Add("Sexual");
            if (filterResults.Violence?.Filtered == true) reasons.Add("Violence");
            if (filterResults.Jailbreak?.Filtered == true) reasons.Add("Jailbreak");
            if (filterResults.Profanity?.Filtered == true) reasons.Add("Profanity");

            return (reasons.Any(), string.Join(", ", reasons));
        }

        private static int EstimateTokenCount(string text)
        {
            // Rough estimation: ~4 characters per token for English text
            return Math.Max(1, text.Length / 4);
        }
    }
}