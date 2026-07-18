# ResumeSpy — Backend API

A .NET 10 Web API for AI-powered resume management. Supports resume creation, translation, and customization with multiple AI and translation providers.

## Features

- JWT authentication via [Supabase](https://supabase.com) (OIDC/JWKS)
- AI-powered resume translation (HuggingFace, OpenAI, with provider fallback)
- Multiple translation providers (Microsoft Translator, DeepL, LibreTranslate, AI)
- PostgreSQL via Entity Framework Core (auto-migrations on startup)
- PDF and thumbnail generation (QuestPDF, ImageSharp)
- Rate limiting, security headers, structured request/response logging
- Health check endpoints (`/health`, `/health/db`)
- Sentry error tracking
- CI/CD via GitHub Actions → Render

## Architecture

Clean Architecture with three layers:

| Layer | Project | Responsibility |
|-------|---------|---------------|
| Domain | `ResumeSpy.Core` | Entities, interfaces, business logic |
| Infrastructure | `ResumeSpy.Infrastructure` | EF Core, AI/translation providers, external services |
| Presentation | `ResumeSpy.UI` | Controllers, middleware, DI configuration |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 15+
- A [Supabase](https://supabase.com) project (for authentication)

## Quick Start

```bash
# Clone the repository
git clone https://github.com/feifeijin/ResumeSpy.git
cd ResumeSpy

# Copy and fill in environment variables
cp .env.example .env
# Edit .env with your values

# Restore dependencies
dotnet restore ResumeSpy.sln

# Run the API (migrations apply automatically on startup)
dotnet run --project ResumeSpy.UI
```

The API will be available at `https://localhost:7227`.

## Configuration

All secrets are injected at runtime via environment variables. The `.env.example` file documents every required variable. Key settings:

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__PrimaryDbConnection` | PostgreSQL connection string |
| `Supabase__Url` | Supabase project URL |
| `Supabase__ServiceRoleKey` | Supabase service role key |
| `AI__HuggingFace__ApiToken` | HuggingFace API token (default AI provider) |
| `Cors__AllowedOrigins__0` | First allowed CORS origin (add `__1`, `__2`, … for more) |
| `AllowedHosts` | Semicolon-separated allowed host headers |

See [docs/ENVIRONMENTS.md](docs/ENVIRONMENTS.md) for a full configuration reference.

## Running Tests

```bash
dotnet test ResumeSpy.sln
```

## Database Migrations

```bash
# Add a new migration
dotnet ef migrations add MigrationName \
  --project ResumeSpy.Infrastructure \
  --startup-project ResumeSpy.UI

# Apply manually (migrations also run automatically on startup)
dotnet ef database update \
  --project ResumeSpy.Infrastructure \
  --startup-project ResumeSpy.UI
```

## Project Structure

```
ResumeSpy.Core/          Domain entities and interfaces
ResumeSpy.Infrastructure/ EF Core, AI/translation service implementations
ResumeSpy.UI/            ASP.NET Core app, controllers, middleware
ResumeSpy.Tests/         xUnit test suite
docs/                    Environment and feature documentation
.github/workflows/       CI (build+test) and CD (deploy to Render)
```

## CI/CD

| Workflow | Trigger | Action |
|----------|---------|--------|
| CI | Pull request | Build + test |
| CD (DEV) | Push to `main` | Deploy to DEV environment on Render |
| CD (PROD) | Push to `release/**` | Deploy to PROD environment on Render |
| Heartbeat | Cron (Mon/Thu) | Keep Supabase free tier active |

Required GitHub repository secrets and variables are documented in [docs/ENVIRONMENTS.md](docs/ENVIRONMENTS.md).

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## Security

To report a vulnerability, see [SECURITY.md](SECURITY.md).

## License

[MIT](LICENSE)
