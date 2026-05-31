using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using ResumeSpy.Core.AI;
using ResumeSpy.Core.Interfaces.AI;
using System.Diagnostics;

namespace ResumeSpy.Infrastructure.Services.AI
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

        // Hard daily spend cap (USD). 0 = disabled. Enforced per UTC day across
        // the process — the service is registered as a keyed singleton, so this
        // counter is shared by every caller. The cap is a runaway-cost
        // backstop, not fine-grained per-user accounting; per-user throttling
        // already lives in the "ai" rate limiter and IAiQuotaService.
        private readonly decimal _maxDailySpendUsd;
        private readonly object _spendLock = new();
        private DateOnly _spendDayUtc;
        private decimal _spentTodayUsd;

        public OpenAITextService(IConfiguration configuration, ILogger<OpenAITextService> logger)
        {
            _logger = logger;

            var endpoint = GetRequiredConfiguration(configuration, "AI:OpenAI:Endpoint", "OpenAI endpoint not configured");
            var apiKey = GetRequiredConfiguration(configuration, "AI:OpenAI:ApiKey", "OpenAI API key not configured");

            _defaultModel = configuration["AI:OpenAI:DefaultModel"] ?? "gpt-4o-mini";

            // Pricing per 1K tokens (update based on your model)
            var inputCostStr = configuration["AI:OpenAI:InputTokenCostPer1K"] ?? "0.00015";
            var outputCostStr = configuration["AI:OpenAI:OutputTokenCostPer1K"] ?? "0.0006";

            _inputTokenCostPer1K = decimal.TryParse(inputCostStr, out var inputCost) ? inputCost : 0.00015m;
            _outputTokenCostPer1K = decimal.TryParse(outputCostStr, out var outputCost) ? outputCost : 0.0006m;

            var capStr = configuration["AI:OpenAI:MaxDailySpendUsd"];
            _maxDailySpendUsd = decimal.TryParse(capStr, out var cap) && cap > 0 ? cap : 0m;
            _spendDayUtc = DateOnly.FromDateTime(DateTime.UtcNow);

            _client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        }

        private static string GetRequiredConfiguration(IConfiguration configuration, string key, string errorMessage)
        {
            var value = configuration[key];
            return !string.IsNullOrWhiteSpace(value) ? value : throw new InvalidOperationException(errorMessage);
        }

        public async Task<AIResponse> GenerateResponseAsync(AIRequest request, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var modelToUse = request.ModelId ?? _defaultModel;

            if (IsDailySpendCapExceeded(out var spentToday))
            {
                stopwatch.Stop();
                _logger.LogError(
                    "OpenAI daily spend cap exceeded (spent ${Spent:F4} of ${Cap:F2}). Refusing request so the orchestrator falls back.",
                    spentToday, _maxDailySpendUsd);
                return new AIResponse
                {
                    IsSuccess = false,
                    ErrorMessage = $"OpenAI daily spend cap of ${_maxDailySpendUsd:F2} reached (spent ${spentToday:F4}). Try again after UTC midnight.",
                    Latency = stopwatch.Elapsed,
                    ProviderName = "OpenAI",
                    ModelUsed = modelToUse
                };
            }

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

                var completion = await chatClient.CompleteChatAsync(messages, options, cancellationToken);

                stopwatch.Stop();

                var usage = completion.Value.Usage;
                var cost = CalculateCost(usage.InputTokenCount, usage.OutputTokenCount);
                var spendAfter = AccrueSpend(cost);

                _logger.LogInformation(
                    "OpenAI request completed. Model: {Model}, Tokens: {TotalTokens} ({InputTokens} in + {OutputTokens} out), Cost: ${Cost:F4}, SpentTodayUsd: ${SpentToday:F4}, Latency: {Latency}ms",
                    modelToUse, usage.TotalTokenCount, usage.InputTokenCount, usage.OutputTokenCount, cost, spendAfter, stopwatch.ElapsedMilliseconds);

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

        private bool IsDailySpendCapExceeded(out decimal spentToday)
        {
            if (_maxDailySpendUsd <= 0m)
            {
                spentToday = 0m;
                return false;
            }

            lock (_spendLock)
            {
                RollDayIfNeeded();
                spentToday = _spentTodayUsd;
                return _spentTodayUsd >= _maxDailySpendUsd;
            }
        }

        private decimal AccrueSpend(decimal cost)
        {
            lock (_spendLock)
            {
                RollDayIfNeeded();
                _spentTodayUsd += cost;
                return _spentTodayUsd;
            }
        }

        private void RollDayIfNeeded()
        {
            var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
            if (todayUtc != _spendDayUtc)
            {
                _spendDayUtc = todayUtc;
                _spentTodayUsd = 0m;
            }
        }
    }
}