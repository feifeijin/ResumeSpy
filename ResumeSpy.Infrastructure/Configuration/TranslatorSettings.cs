namespace ResumeSpy.Infrastructure.Configuration
{
    public class TranslatorSettings
    {
        public TranslatorType TranslatorType { get; set; }
        public MicrosoftTranslatorSettings? Microsoft { get; set; }
        public DeepLTranslatorSettings? DeepL { get; set; }
        public LibreTranslatorSettings? Libre { get; set; }
        public AITranslatorSettings? AI { get; set; }
    }

    public class MicrosoftTranslatorSettings
    {
        public required string SubscriptionKey { get; set; }
        public required string Endpoint { get; set; }
    }

    public class DeepLTranslatorSettings
    {
        public required string AuthKey { get; set; }
        public required string Endpoint { get; set; }
    }

    public class LibreTranslatorSettings
    {
        public required string ApiKey { get; set; }
        public required string Endpoint { get; set; }
    }

    public class AITranslatorSettings
    {
        /// <summary>
        /// AI provider to use for translation (defaults to configured AI provider chain)
        /// </summary>
        public string? PreferredProvider { get; set; }

        /// <summary>
        /// Whether to use AI orchestrator's fallback chain
        /// </summary>
        public bool UseFallbackChain { get; set; } = true;

        /// <summary>
        /// Context to provide for better translation quality
        /// </summary>
        public string? DefaultContext { get; set; }
    }
}
