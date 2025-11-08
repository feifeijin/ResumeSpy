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
`AIOrchestratorService` handles provider fallback, caching, and request routing. Always use this for AI operations rather than calling providers directly.

### Repository Pattern with Unit of Work
All data operations go through `IUnitOfWork` and repository interfaces. PostgreSQL is used via Entity Framework with `ApplicationDbContext`.

### Configuration-Driven Provider Selection
AI and translation providers are selected via `appsettings.json`:
- `AI:DefaultTextProvider` and `AI:TextProviderFallbackChain` control AI routing
- `TranslatorSettings:TranslatorType` selects translation provider (Microsoft/DeepL/Libre)

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

### Entity Conventions
- All entities inherit from `Base<T>` with timestamp columns configured as PostgreSQL `timestamp` type
- ViewModels are in `ResumeSpy.Core.Entities.Business` 
- Domain entities are in `ResumeSpy.Core.Entities.General`

### Service Registration
All services are registered in `ServiceExtension.RegisterService()` with specific patterns:
- Services use `AddScoped`
- AI providers use `AddKeyedSingleton` 
- AutoMapper uses custom `BaseMapper<T, U>` wrapper

### AI Integration
- All AI requests use `AIRequest`/`AIResponse` DTOs
- Cache keys are SHA256 hashes of request parameters
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
- **Translation APIs**: Microsoft Translator, DeepL, LibreTranslate
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

### CORS Configuration
Configured for multiple origins including localhost:5173 (frontend) and API endpoints.

### AI Provider Configuration
Each AI provider requires specific configuration sections with endpoints, API keys, and model parameters.