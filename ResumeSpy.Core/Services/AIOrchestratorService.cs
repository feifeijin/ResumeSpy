using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ResumeSpy.Core.AI;
using ResumeSpy.Core.Interfaces.AI;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ResumeSpy.Core.Services
{
    /// <summary>
    /// Central orchestrator for AI operations with provider selection, fallback, and caching
    /// </summary>
    public class AIOrchestratorService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AIOrchestratorService> _logger;

        public AIOrchestratorService(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            IMemoryCache cache,
            ILogger<AIOrchestratorService> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Execute a text generation request with automatic provider selection and fallback
        /// </summary>
        public async Task<AIResponse> ExecuteTextGenerationAsync(AIRequest request, bool useCache = true)
        {
            // Generate cache key
            var cacheKey = useCache ? GenerateCacheKey(request) : null;

            // Check cache
            if (cacheKey != null && _cache.TryGetValue<AIResponse>(cacheKey, out var cachedResponse) && cachedResponse != null)
            {
                _logger.LogInformation("Cache hit for request");
                return cachedResponse;
            }

            // Get provider fallback chain from configuration
            // var providers = _configuration.GetSection("AI:TextProviderFallbackChain").Value.Split(',')
            //     ?? new[] { "OpenAI" };

            var providers = _configuration.GetSection("AI:TextProviderFallbackChain").Value?.Split(',')
            ?? new[] { "OpenAI" };

            AIResponse? response = null;
            Exception? lastException = null;

            foreach (var providerName in providers)
            {
                try
                {
                    _logger.LogInformation("Attempting provider: {Provider}", providerName);

                    var service = _serviceProvider.GetKeyedService<IGenerativeTextService>(providerName);

                    if (service == null)
                    {
                        _logger.LogWarning("Provider {Provider} is not registered", providerName);
                        continue;
                    }

                    response = await service.GenerateResponseAsync(request);

                    if (response.IsSuccess)
                    {
                        _logger.LogInformation("Request succeeded with provider: {Provider}", providerName);

                        // Cache successful response
                        if (cacheKey != null)
                        {
                            var cacheExpiration = TimeSpan.FromMinutes(
                                _configuration.GetSection("AI:CacheExpirationMinutes").Value != null ?
                                int.Parse(_configuration.GetSection("AI:CacheExpirationMinutes").Value) : 60);

                            _cache.Set(cacheKey, response, cacheExpiration);
                        }

                        return response;
                    }

                    _logger.LogWarning("Provider {Provider} returned failure: {Error}", providerName, response.ErrorMessage);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogError(ex, "Provider {Provider} threw exception", providerName);
                }
            }

            // All providers failed
            _logger.LogError("All providers failed for text generation request");

            return response ?? new AIResponse
            {
                IsSuccess = false,
                ErrorMessage = $"All providers failed. Last error: {lastException?.Message ?? "Unknown error"}",
                ProviderName = "None"
            };
        }

        /// <summary>
        /// Get the AI-powered translation service
        /// </summary>
        public IAITranslationService GetTranslationService()
        {
            // Get the default text provider
            var providerName = _configuration["AI:DefaultTextProvider"] ?? "OpenAI";
            var textService = _serviceProvider.GetRequiredKeyedService<IGenerativeTextService>(providerName);

            // Create translation service with the selected provider
            var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
            return new Infrastructure.AI.AITranslationService(
                textService);
        }

        /// <summary>
        /// Generate a cache key based on request parameters
        /// </summary>
        private static string GenerateCacheKey(AIRequest request)
        {
            var keyData = new
            {
                request.Prompt,
                request.SystemMessage,
                request.Temperature,
                request.MaxTokens,
                request.ModelId
            };

            var json = JsonSerializer.Serialize(keyData);
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));

            return $"ai_cache_{Convert.ToHexString(hash)}";
        }
    }
}
