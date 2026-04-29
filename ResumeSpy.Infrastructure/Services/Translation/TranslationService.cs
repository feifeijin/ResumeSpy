using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.Infrastructure.Configuration;
using ResumeSpy.Infrastructure.Services.AI;

namespace ResumeSpy.Infrastructure.Services.Translation
{
    /// <summary>
    /// Implementation of ITranslationService that bridges business layer to infrastructure.
    /// Provides clean abstraction over different translation providers and caches results
    /// in memory to avoid redundant network calls for identical translation requests.
    /// </summary>
    public class TranslationService : ITranslationService
    {
        private readonly TranslatorFactory _translatorFactory;
        private readonly IMemoryCache _cache;

        // Cache translations for 30 minutes — resume content changes infrequently and
        // free-tier translation providers are both slow and rate-limited.
        private static readonly TimeSpan TranslationCacheDuration = TimeSpan.FromMinutes(30);

        public TranslationService(
            IHttpClientFactory httpClientFactory,
            IOptions<TranslatorSettings> translatorSettings,
            IMemoryCache cache,
            AIOrchestratorService? aiOrchestrator = null)
        {
            _translatorFactory = new TranslatorFactory(httpClientFactory, translatorSettings, aiOrchestrator);
            _cache = cache;
        }

        public async Task<string> DetectLanguageAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var translator = _translatorFactory.CreateTranslator();
            return await translator.DetectLanguageAsync(text);
        }

        public async Task<string> TranslateTextAsync(string text, string sourceLanguage, string targetLanguage)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            if (string.IsNullOrWhiteSpace(targetLanguage))
                return text;

            // If source and target are the same, no translation needed
            if (sourceLanguage.Equals(targetLanguage, StringComparison.OrdinalIgnoreCase))
                return text;

            var cacheKey = BuildTranslationCacheKey(text, sourceLanguage, targetLanguage);
            if (_cache.TryGetValue<string>(cacheKey, out var cached) && cached != null)
                return cached;

            var translator = _translatorFactory.CreateTranslator();
            var result = await translator.TranslateAsync(text, sourceLanguage, targetLanguage);

            _cache.Set(cacheKey, result, TranslationCacheDuration);
            return result;
        }

        public async Task<string> TranslateTextAsync(string text, string targetLanguage)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(targetLanguage))
                return text;

            // Auto-detect source language first
            var sourceLanguage = await DetectLanguageAsync(text);

            if (string.IsNullOrWhiteSpace(sourceLanguage))
                sourceLanguage = "en"; // Default to English if detection fails

            return await TranslateTextAsync(text, sourceLanguage, targetLanguage);
        }

        /// <summary>
        /// Builds a deterministic cache key from the translation inputs.
        /// A SHA-256 hash is used so long texts do not bloat the key.
        /// </summary>
        private static string BuildTranslationCacheKey(string text, string sourceLanguage, string targetLanguage)
        {
            var raw = $"{sourceLanguage}|{targetLanguage}|{text}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return $"translation_{Convert.ToHexString(hash)}";
        }
    }
}
