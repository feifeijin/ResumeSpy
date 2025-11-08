using System;
using System.Net.Http;
using Microsoft.Extensions.Options;
using ResumeSpy.Infrastructure.Configuration;
using ResumeSpy.Infrastructure.Services.AI;

namespace ResumeSpy.Infrastructure.Services.Translation
{
    internal class TranslatorFactory
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TranslatorSettings _translatorSettings;
        private readonly AIOrchestratorService? _aiOrchestrator;

        public TranslatorFactory(
            IHttpClientFactory httpClientFactory, 
            IOptions<TranslatorSettings> translatorSettings,
            AIOrchestratorService? aiOrchestrator = null)
        {
            _httpClientFactory = httpClientFactory;
            _translatorSettings = translatorSettings.Value;
            _aiOrchestrator = aiOrchestrator;
        }

        public ITranslator CreateTranslator()
        {
            var httpClient = _httpClientFactory.CreateClient();

            return _translatorSettings.TranslatorType switch
            {
                TranslatorType.Microsoft => new MicrosoftTranslator(
                    httpClient, 
                    _translatorSettings.Microsoft?.SubscriptionKey ?? throw new InvalidOperationException("Microsoft translator settings are required"), 
                    _translatorSettings.Microsoft.Endpoint),
                    
                TranslatorType.DeepL => new DeepLTranslator(
                    httpClient, 
                    _translatorSettings.DeepL?.AuthKey ?? throw new InvalidOperationException("DeepL translator settings are required"), 
                    _translatorSettings.DeepL.Endpoint),
                    
                TranslatorType.Libre => new LibreTranslator(
                    httpClient, 
                    _translatorSettings.Libre?.ApiKey ?? throw new InvalidOperationException("Libre translator settings are required"), 
                    _translatorSettings.Libre.Endpoint),

                TranslatorType.AI => new AITranslator(
                    _aiOrchestrator ?? throw new InvalidOperationException("AI orchestrator is required for AI translator"),
                    _translatorSettings.AI),
                    
                _ => throw new Exception("Invalid translator type specified in configuration.")
            };
        }
    }
}
