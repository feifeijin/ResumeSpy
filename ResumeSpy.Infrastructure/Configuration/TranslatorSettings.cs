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
        /// Context to provide for better translation quality. Provider selection
        /// and fallback chain are owned by the top-level <c>AI</c> config section
        /// (see <c>AI:DefaultTextProvider</c> / <c>AI:TextProviderFallbackChain</c>);
        /// translator-side overrides would just re-introduce the inconsistency
        /// that issue #36 cleaned up.
        /// </summary>
        public string? DefaultContext { get; set; }
    }
}
