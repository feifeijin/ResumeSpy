using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using ResumeSpy.Core.AI;
using ResumeSpy.Core.Interfaces.AI;
using System.Diagnostics;

namespace ResumeSpy.Infrastructure.AI
{
    /// <summary>
    /// OpenAI/Azure OpenAI adapter for generative text services
    /// </summary>
    public class OpenAITextService : IGenerativeTextService
    {
        private readonly AzureOpenAIClient _client;
        private readonly ILogger<OpenAITextService> _logger;
        private readonly string _defaultModel;
        private readonly decimal _inputTokenCostPer1K;
        private readonly decimal _outputTokenCostPer1K;

        public OpenAITextService(IConfiguration configuration, ILogger<OpenAITextService> logger)
        {
            _logger = logger;

            var endpoint = configuration["AI:OpenAI:Endpoint"] 
                ?? throw new InvalidOperationException("OpenAI endpoint not configured");
            var apiKey = configuration["AI:OpenAI:ApiKey"] 
                ?? throw new InvalidOperationException("OpenAI API key not configured");
            
            _defaultModel = configuration["AI:OpenAI:DefaultModel"] ?? "gpt-4o-mini";
            
            // Pricing per 1K tokens (update based on your model)
            _inputTokenCostPer1K = decimal.Parse(configuration["AI:OpenAI:InputTokenCostPer1K"] ?? "0.00015");
            _outputTokenCostPer1K = decimal.Parse(configuration["AI:OpenAI:OutputTokenCostPer1K"] ?? "0.0006");

            _client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        }

        public async Task<AIResponse> GenerateResponseAsync(AIRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var modelToUse = request.ModelId ?? _defaultModel;

            try
            {
                var chatClient = _client.GetChatClient(modelToUse);

                var messages = new List<ChatMessage>();
                
                if (!string.IsNullOrWhiteSpace(request.SystemMessage))
                {
                    messages.Add(new SystemChatMessage(request.SystemMessage));
                }
                
                messages.Add(new UserChatMessage(request.Prompt));

                var options = new ChatCompletionOptions
                {
                    Temperature = (float)request.Temperature,
                    MaxOutputTokenCount = request.MaxTokens
                };

                var completion = await chatClient.CompleteChatAsync(messages, options);

                stopwatch.Stop();

                var usage = completion.Value.Usage;
                var cost = CalculateCost(usage.InputTokenCount, usage.OutputTokenCount);

                _logger.LogInformation(
                    "OpenAI request completed. Model: {Model}, Tokens: {TotalTokens} ({InputTokens} in + {OutputTokens} out), Cost: ${Cost:F4}, Latency: {Latency}ms",
                    modelToUse, usage.TotalTokenCount, usage.InputTokenCount, usage.OutputTokenCount, cost, stopwatch.ElapsedMilliseconds);

                return new AIResponse
                {
                    Content = completion.Value.Content[0].Text,
                    IsSuccess = true,
                    Cost = cost,
                    Latency = stopwatch.Elapsed,
                    PromptTokens = usage.InputTokenCount,
                    CompletionTokens = usage.OutputTokenCount,
                    ProviderName = "OpenAI",
                    ModelUsed = modelToUse
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "OpenAI request failed. Model: {Model}, Latency: {Latency}ms", modelToUse, stopwatch.ElapsedMilliseconds);

                return new AIResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Latency = stopwatch.Elapsed,
                    ProviderName = "OpenAI",
                    ModelUsed = modelToUse
                };
            }
        }

        private decimal CalculateCost(int inputTokens, int outputTokens)
        {
            var inputCost = (inputTokens / 1000m) * _inputTokenCostPer1K;
            var outputCost = (outputTokens / 1000m) * _outputTokenCostPer1K;
            return inputCost + outputCost;
        }
    }
}
