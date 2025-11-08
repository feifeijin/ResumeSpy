# ResumeSpy AI Coding Instructions

## Architecture Overview

ResumeSpy is a .NET 9.0 Web API for resume management with AI-powered translation and customization. It follows Clean Architecture with three layers:

- **ResumeSpy.Core**: Domain entities, interfaces, and business logic
- **ResumeSpy.Infrastructure**: Data access, external services (AI, translation), and implementations
- **ResumeSpy.UI**: Web API controllers, middleware, and dependency injection setup

## Key Architectural Patterns

### Dependency Injection with Keyed Services
AI services use keyed registration for provider fallback:
```csharp
services.AddKeyedSingleton<IGenerativeTextService, OpenAITextService>("OpenAI");
var service = _serviceProvider.GetKeyedService<IGenerativeTextService>("OpenAI");
```

### AI Orchestrator Pattern
`AIOrchestratorService` (in `Infrastructure/Services/AI`) handles provider fallback, caching, and request routing. Always use this for AI operations rather than calling providers directly.

### Translation Service Architecture
Multiple translation providers available via configuration:
- **Traditional**: Microsoft Translator, DeepL, LibreTranslate 
- **AI-powered**: Uses AI orchestrator with configured AI providers
- Selection via `TranslatorSettings:TranslatorType` enum (`Microsoft`, `DeepL`, `Libre`, `AI`)

### Repository Pattern with Unit of Work
All data operations go through `IUnitOfWork` and repository interfaces. PostgreSQL is used via Entity Framework with `ApplicationDbContext`.

## Critical Development Workflows

### Database Operations
```bash
# Add migration
dotnet ef migrations add MigrationName --project ResumeSpy.Infrastructure --startup-project ResumeSpy.UI

# Update database  
dotnet ef database update --project ResumeSpy.Infrastructure --startup-project ResumeSpy.UI
```

### Build and Run
Use VS Code tasks or:
```bash
dotnet build ResumeSpy.sln
dotnet run --project ResumeSpy.UI
```

## Project-Specific Conventions

### Service Organization
- **Business Logic**: `ResumeSpy.Core/Services`
- **Infrastructure Services**: `ResumeSpy.Infrastructure/Services/`
  - `AI/`: AI orchestrator and providers
  - `Translation/`: Translation service implementations
- **Registration**: All in `ServiceExtension.RegisterService()` with specific patterns

### Entity Conventions
- All entities inherit from `Base<T>` with timestamp columns configured as PostgreSQL `timestamp` type
- ViewModels are in `ResumeSpy.Core.Entities.Business` 
- Domain entities are in `ResumeSpy.Core.Entities.General`

### AI & Translation Integration
- AI requests use `AIRequest`/`AIResponse` DTOs with SHA256 cache keys
- Translation uses factory pattern with enum-based provider selection
- AI translation combines text generation with language-specific prompts
- Provider fallback is automatic through configuration

### API Controllers
- Controllers are in `ResumeSpy.UI/Controllers`
- Use `[ApiController]` and `[Route("api/[controller]")]`
- Inject `IMemoryCache` for caching patterns
- Use `X.PagedList` for pagination

## Integration Points

### External Dependencies
- **PostgreSQL**: Primary database via Npgsql provider
- **Azure OpenAI**: Text generation via `Azure.AI.OpenAI` package  
- **Translation APIs**: Microsoft Translator, DeepL, LibreTranslate, AI-powered
- **QuestPDF**: PDF generation for resumes
- **ImageSharp**: Image processing for resume thumbnails

### Cross-Component Communication
- Services communicate through interfaces defined in `ResumeSpy.Core.Interfaces`
- AI operations are centralized through `AIOrchestratorService`
- Translation services implement `ITranslationService` with provider abstraction

## Configuration Requirements

### Connection Strings
PostgreSQL connection in `appsettings.json`:
```json
"ConnectionStrings": {
  "PrimaryDbConnection": "server=YourServer; database=YourDatabase; Trusted_Connection=True;TrustServerCertificate=True"
}
```

### Translation Configuration
```json
"TranslatorSettings": {
  "TranslatorType": "AI",
  "AI": {
    "PreferredProvider": "OpenAI",
    "UseFallbackChain": true,
    "DefaultContext": "Professional resume context..."
  }
}
```

### CORS Configuration
Configured for multiple origins including localhost:5173 (frontend) and API endpoints.

### AI Provider Configuration
Each AI provider requires specific configuration sections with endpoints, API keys, and model parameters.