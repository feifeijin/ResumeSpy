using System;
using System.Net.Http;
using Microsoft.Extensions.Options;
using ResumeSpy.Configuration;
using ResumeSpy.Interfaces;

namespace ResumeSpy.Services
{
    public class TranslatorFactory
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TranslatorSettings _translatorSettings;

        public TranslatorFactory(IHttpClientFactory httpClientFactory, IOptions<TranslatorSettings> translatorSettings)
        {
            _httpClientFactory = httpClientFactory;
            _translatorSettings = translatorSettings.Value;
        }

        public ITranslator CreateTranslator()
        {
            var httpClient = _httpClientFactory.CreateClient();

            return _translatorSettings.TranslatorType switch
            {
                TranslatorType.Microsoft => new MicrosoftTranslator(httpClient, _translatorSettings.Microsoft.SubscriptionKey, _translatorSettings.Microsoft.Endpoint),
                TranslatorType.DeepL => new DeepLTranslator(httpClient, _translatorSettings.DeepL.AuthKey),
                TranslatorType.Libre => new LibreTranslator(httpClient, _translatorSettings.Libre.ApiKey),
                _ => throw new Exception("Invalid translator type specified in configuration.")
            };
        }
    }
}