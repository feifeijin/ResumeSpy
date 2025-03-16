namespace ResumeSpy.Configuration
{
    public class TranslatorSettings
    {
        public TranslatorType TranslatorType { get; set; }
        public MicrosoftTranslatorSettings Microsoft { get; set; }
        public DeepLTranslatorSettings DeepL { get; set; }
        public LibreTranslatorSettings Libre { get; set; }
    }

    public class MicrosoftTranslatorSettings
    {
        public string SubscriptionKey { get; set; }
        public string Endpoint { get; set; }
    }

    public class DeepLTranslatorSettings
    {
        public string AuthKey { get; set; }

        public string Endpoint { get; set; }

    }

    public class LibreTranslatorSettings
    {
        public string ApiKey { get; set; }

        public string Endpoint { get; set; }

    }
}