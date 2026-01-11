# ResumeSpy

[![CI Status](https://github.com/feifeijin/ResumeSpy/workflows/CI%20-%20Pull%20Request%20Validation/badge.svg)](https://github.com/feifeijin/ResumeSpy/actions/workflows/ci.yml)
[![CD Status](https://github.com/feifeijin/ResumeSpy/workflows/CD%20-%20Continuous%20Deployment/badge.svg)](https://github.com/feifeijin/ResumeSpy/actions/workflows/cd.yml)
[![.NET Version](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

ResumeSpy is a back-end application designed to efficiently manage resumes with version control, multi-language support, and customization for specific job descriptions (JDs).

## Features
- **Single Source Maintenance**: Manage one primary language version for each resume position.
- **Version Control**: Integrated with Git to track changes and compare versions.
- **Multi-language Support**: Support for translating resumes to various languages.
- **JD-specific Customization**: Allows resume creation tailored for job descriptions.
- **User Authentication**: JWT-based API authentication with refresh tokens plus Google and GitHub social login support.
- **Interactive API Docs**: Built-in Swagger UI available at `/swagger.html` for exploring endpoints.

## Technology Stack

- **Back-End**: .NET 9.0 Web API
- **Database**: PostgreSQL 16+
- **ORM**: Entity Framework Core
- **Authentication**: JWT + OAuth (Google, GitHub)
- **Translation**: Multiple providers (Microsoft Translator, DeepL, LibreTranslate, AI-powered)
- **AI Services**: Azure OpenAI, OpenAI
- **Version Control**: Git for tracking resume versions
- **CI/CD**: GitHub Actions

## Authentication Overview

ResumeSpy now exposes a dedicated authentication module that supports both traditional email/password accounts and social sign-in with Google or GitHub. Successful authentication returns a short-lived access token and a rolling refresh token. Clients are expected to:

1. Store the returned `accessToken` and `refreshToken` values securely.
2. Attach the bearer access token to subsequent API requests (`Authorization: Bearer <token>`).
3. Call `POST /api/auth/refresh` with the refresh token when the access token expires to obtain a new token pair.
4. Call `POST /api/auth/logout` to invalidate a refresh token when the user signs out.

### API Endpoints

| Endpoint | Description |
| --- | --- |
| `POST /api/auth/register` | Create a new local account (email + password). |
| `POST /api/auth/login` | Authenticate via email/password. |
| `POST /api/auth/refresh` | Exchange a refresh token for a new access/refresh pair. |
| `POST /api/auth/external` | Complete a social sign-in using Google (ID token) or GitHub (access token). |
| `POST /api/auth/logout` | Revoke a refresh token (requires authentication). |

### Configuration

Add the following sections to your `appsettings.*.json` files and populate them with real values before running the API:

```json
"Jwt": {
    "Issuer": "ResumeSpy",
    "Audience": "ResumeSpyFrontend",
    "SigningKey": "<32+ character secure key>",
    "AccessTokenDurationInMinutes": 60,
    "RefreshTokenDurationInDays": 14
},
"ExternalAuth": {
    "Google": {
        "ClientId": "<google-oauth-client-id>"
    },
    "Github": {
        "ClientId": "<github-oauth-client-id>",
        "ClientSecret": "<github-oauth-client-secret>"
    }
}
```

> ⚠️ Keep the signing key and OAuth secrets out of source control. Use user secrets or environment variables in production.

### API Documentation

- Start the API (`dotnet run --project ResumeSpy.UI`).
- Open `https://localhost:7227/swagger.html` (or the base URL you run on) to view the interactive documentation and try endpoints with JWT auth.

## CI/CD Pipeline

ResumeSpy uses GitHub Actions for automated continuous integration and deployment.

### Workflow Diagram

```
Pull Request → [CI Workflow] → Build & Test → Merge
                                                 ↓
                                            main branch
                                                 ↓
                                        [CD Workflow]
                                                 ↓
                                        Build & Publish
                                                 ↓
                                          Deploy to DEV
                                          (stub implementation)
```

```
Release Branch → [CD Workflow] → Build & Test → Publish → Deploy to PROD
                                                           (stub implementation)
```

### CI Workflow (Pull Requests)

**Triggers**: Pull requests to `main` or `release/**` branches

**Steps**:
1. ✅ Checkout code
2. ✅ Setup .NET 9.0
3. ✅ Restore dependencies
4. ✅ Build solution (Release mode)
5. ✅ Run tests
6. ✅ Report status

**Purpose**: Validate that code builds and tests pass before merging.

### CD Workflow (Deployments)

**Triggers**: 
- Push to `main` → Deploy to DEV
- Push to `release/**` → Deploy to PROD

**Steps**:
1. ✅ Build and test
2. ✅ Publish application artifacts
3. ⚠️ Deploy to environment (stub implementation)

**Current Status**: Deployment steps are **stubs** (placeholders). See [Implementing Real Deployment](#implementing-real-deployment) to configure actual deployment.

## Environment Configuration

ResumeSpy supports multiple environments with separate configuration:

| Environment | Branch | Database | Deployment |
|------------|--------|----------|------------|
| **Local** | Any | Local PostgreSQL | Manual (`dotnet run`) |
| **DEV** | `main` | Hosted PostgreSQL | Automatic via CD workflow |
| **PROD** | `release/**` | Production PostgreSQL | Automatic via CD workflow (manual approval) |

### Environment Setup

1. **Copy environment template**
   ```bash
   cp .env.example .env
   ```

2. **Configure secrets** (see [docs/ENVIRONMENTS.md](docs/ENVIRONMENTS.md))
   - Database connection string
   - JWT signing key
   - OAuth credentials (Google, GitHub)
   - CORS origins

3. **Apply database migrations**
   ```bash
   dotnet ef database update \
     --project ResumeSpy.Infrastructure \
     --startup-project ResumeSpy.UI
   ```

For detailed environment configuration, see [docs/ENVIRONMENTS.md](docs/ENVIRONMENTS.md).

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- PostgreSQL 16+ (or Docker)
- Git

### Quick Start

1. **Clone the repository**
   ```bash
   git clone https://github.com/feifeijin/ResumeSpy.git
   cd ResumeSpy
   ```

2. **Start PostgreSQL (using Docker)**
   ```bash
   docker run --name resumespy-postgres \
     -e POSTGRES_PASSWORD=devpassword \
     -e POSTGRES_DB=resumespy_dev \
     -p 5432:5432 \
     -d postgres:16
   ```

3. **Configure user secrets**
   ```bash
   cd ResumeSpy.UI
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:PrimaryDbConnection" "Host=localhost;Port=5432;Database=resumespy_dev;Username=postgres;Password=devpassword;"
   dotnet user-secrets set "Jwt:SigningKey" "your-local-dev-key-minimum-32-characters-long"
   cd ..
   ```

4. **Apply migrations and run**
   ```bash
   dotnet ef database update --project ResumeSpy.Infrastructure --startup-project ResumeSpy.UI
   dotnet restore ResumeSpy.sln
   dotnet build ResumeSpy.sln
   dotnet run --project ResumeSpy.UI
   ```

5. **Access the API**
   - API: `https://localhost:7227`
   - Swagger UI: `https://localhost:7227/swagger.html`

## Project Structure

```
ResumeSpy/
├── .github/
│   └── workflows/          # CI/CD workflows
│       ├── ci.yml         # Pull request validation
│       └── cd.yml         # Continuous deployment
├── docs/
│   ├── CONTRIBUTING.md    # Contributing guidelines
│   ├── DEPLOYMENT.md      # Deployment implementation guide
│   └── ENVIRONMENTS.md    # Environment configuration
├── ResumeSpy.Core/        # Domain layer
│   ├── Entities/          # Domain entities
│   ├── Interfaces/        # Repository interfaces
│   └── Services/          # Business logic
├── ResumeSpy.Infrastructure/  # Infrastructure layer
│   ├── Data/              # Database context
│   ├── Repositories/      # Data access implementations
│   └── Services/          # External service integrations
│       ├── AI/            # AI provider implementations
│       └── Translation/   # Translation services
├── ResumeSpy.UI/          # Presentation layer
│   ├── Controllers/       # API controllers
│   ├── Middleware/        # Custom middleware
│   └── Models/            # API DTOs
└── .env.example           # Environment variables template
```

## Implementing Real Deployment

The current CD workflow includes deployment **stubs** (placeholders). To implement actual deployment:

1. **Choose a deployment platform**:
   - Azure App Service (recommended for enterprise)
   - Railway (recommended for startups)
   - Docker + VPS (cost-effective)
   - AWS Elastic Beanstalk

2. **Configure GitHub Environments**:
   - Go to repository Settings → Environments
   - Create `DEV` and `PROD` environments
   - Add required secrets (deployment tokens, connection strings, etc.)

3. **Update CD workflow**:
   - Replace deployment stub steps with actual deployment commands
   - See [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) for platform-specific instructions

4. **Test deployment**:
   - Merge to `main` to trigger DEV deployment
   - Create `release/v1.0.0` branch to trigger PROD deployment

For detailed deployment instructions, see [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md).

## Contributing

We welcome contributions! Please see [docs/CONTRIBUTING.md](docs/CONTRIBUTING.md) for:
- Development workflow
- Branch naming conventions
- Commit message format
- Code standards
- Pull request process

### Quick Contribution Guide

1. Fork the repository
2. Create a feature branch (`feature/your-feature-name`)
3. Make your changes
4. Commit using [Conventional Commits](https://www.conventionalcommits.org/)
5. Push and create a pull request
6. CI will automatically validate your changes

## Documentation

- [CONTRIBUTING.md](docs/CONTRIBUTING.md) - Contributing guidelines and development workflow
- [ENVIRONMENTS.md](docs/ENVIRONMENTS.md) - Environment configuration and setup
- [DEPLOYMENT.md](docs/DEPLOYMENT.md) - Deployment implementation guide
- [AI_TRANSLATION_IMPLEMENTATION.md](docs/AI_TRANSLATION_IMPLEMENTATION.md) - AI and translation services

## Assumptions & Limitations

### Current Assumptions
- **Backend-only repository**: Frontend is in a separate repository
- **PostgreSQL database**: Requires PostgreSQL 16 or later
- **Environment separation**: DEV and PROD use separate databases
- **OAuth configuration**: Requires separate OAuth apps per environment

### Current Limitations
- ⚠️ **Deployment stubs**: CD workflow publishes artifacts but doesn't deploy them yet
- ⚠️ **No test project**: Test infrastructure needs to be added
- ⚠️ **Manual migrations**: Database migrations need manual application in PROD
- ⚠️ **No preview environments**: Backend doesn't support PR-specific preview environments
- ⚠️ **No monitoring**: Application monitoring and alerting not configured
- ⚠️ **No rollback**: Automated rollback mechanism not implemented

These limitations are intentional for the initial CI/CD foundation and can be addressed incrementally.

## Roadmap

### Phase 1: Foundation (Current)
- ✅ CI/CD workflows (build, test, publish)
- ✅ Environment configuration documentation
- ✅ Deployment stubs

### Phase 2: Deployment
- ⏳ Real deployment implementation
- ⏳ Database migration automation
- ⏳ Health check endpoints
- ⏳ Smoke tests post-deployment

### Phase 3: Testing & Quality
- ⏳ Unit test project setup
- ⏳ Integration tests
- ⏳ Code coverage reporting
- ⏳ Automated security scanning

### Phase 4: Production Readiness
- ⏳ Monitoring and alerting
- ⏳ Automated rollback
- ⏳ Blue-green deployments
- ⏳ Performance testing

### Phase 5: Advanced Features
- ⏳ Feature flags
- ⏳ Canary releases
- ⏳ Multi-region deployment
- ⏳ Load balancing

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- **Documentation**: Check [docs/](docs/) directory
- **Issues**: [GitHub Issues](https://github.com/feifeijin/ResumeSpy/issues)
- **Discussions**: [GitHub Discussions](https://github.com/feifeijin/ResumeSpy/discussions)
