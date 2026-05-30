# AI Translation Service

ResumeSpy supports AI-powered translation alongside traditional translation
providers (Microsoft Translator, DeepL, LibreTranslate). The AI translator
uses the AI orchestrator to translate text with awareness of resume context.

## Architecture

| Layer | Component | Responsibility |
| --- | --- | --- |
| Core | `TranslatorType` enum | Adds `AI` as a selectable provider. |
| Core | `AITranslatorSettings` | Preferred provider, fallback chain, default context. |
| Infrastructure | `AIOrchestratorService` (`Infrastructure/Services/AI`) | Provider routing, caching, fallback. |
| Infrastructure | `OpenAITextService`, `AITranslationService` (`Infrastructure/Services/AI`) | Provider implementations. |
| Infrastructure | `AITranslator` | Implements `ITranslator` using the AI orchestrator. |

The translation service factory selects the implementation based on
`TranslatorSettings:TranslatorType`. AI orchestration uses the same fallback
chain configured for general AI requests.

## Configuration

Enable AI translation by setting `TranslatorType` to `"AI"` in
`appsettings.json` (or via the `TranslatorSettings__TranslatorType` env var):

```json
{
  "TranslatorSettings": {
    "TranslatorType": "AI",
    "AI": {
      "PreferredProvider": "OpenAI",
      "UseFallbackChain": true,
      "DefaultContext": "This is a professional resume document. Please maintain formal language and technical accuracy."
    }
  }
}
```

The AI provider used must also be configured under the `AI` section (see
`.env.example` for provider environment variables).

## Usage

The AI translator is invoked through the existing `ITranslationService`
abstraction; controllers do not depend on the specific provider:

```csharp
public class ResumeController : ControllerBase
{
    private readonly ITranslationService _translationService;

    public ResumeController(ITranslationService translationService)
    {
        _translationService = translationService;
    }

    [HttpPost("translate")]
    public async Task<IActionResult> TranslateText([FromBody] TranslationRequest request)
    {
        var translated = await _translationService.TranslateTextAsync(
            request.Text,
            request.TargetLanguage);

        return Ok(new { translated });
    }
}
```

## Behavior

- Requests are cached by the AI orchestrator using SHA-256 keys to avoid
  duplicate API calls for the same input.
- If the preferred provider fails, the orchestrator falls back to other
  configured providers when `UseFallbackChain` is `true`.
- `DefaultContext` is prepended to translation prompts so the model preserves
  professional tone and technical terminology common to resumes.

## Extending

To add a new AI provider:

1. Implement `IGenerativeTextService` in `Infrastructure/Services/AI`.
2. Register it as a keyed singleton in `ServiceExtension.RegisterService()`.
3. Add the provider name to the configured fallback chain.

To customize translation prompting per language or document type, override the
context passed to `AITranslationService` rather than editing `DefaultContext`
globally.
