using System.Net.Http;
using System.Threading.Tasks;

namespace ResumeSpy.Infrastructure.Services.Translation
{
    /// <summary>
    /// Internal interface for translator implementations
    /// Used only within Infrastructure layer
    /// </summary>
    internal interface ITranslator
    {
        Task<string> DetectLanguageAsync(string text);
        Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage);
    }
}
