# Contributing to ResumeSpy

Thank you for your interest in contributing.

## Development Setup

1. Clone the repository and copy the environment template:
   ```bash
   git clone https://github.com/feifeijin/ResumeSpy.git
   cd ResumeSpy
   cp .env.example .env
   # Fill in your local values in .env
   ```

2. Start a local PostgreSQL instance (Docker recommended):
   ```bash
   docker run --name resumespy-postgres \
     -e POSTGRES_PASSWORD=devpassword \
     -e POSTGRES_DB=resumespy_dev \
     -p 5432:5432 \
     -d postgres:16
   ```

3. Restore and run:
   ```bash
   dotnet restore ResumeSpy.sln
   dotnet run --project ResumeSpy.UI
   ```

## Making Changes

- Follow the Clean Architecture layer boundaries: no outward dependencies from `ResumeSpy.Core`.
- All new services must be registered in `ResumeSpy.UI/Extensions/ServiceExtension.cs`.
- Use structured logging message templates (`{Key}`) — no string interpolation in `_logger` calls.
- Sensitive headers must be redacted in `RequestResponseLoggingMiddleware` — add any new ones to the redaction list.

## Adding Database Migrations

```bash
dotnet ef migrations add YourMigrationName \
  --project ResumeSpy.Infrastructure \
  --startup-project ResumeSpy.UI
```

## Running Tests

```bash
dotnet test ResumeSpy.sln
```

All pull requests must pass CI (build + tests) before merging.

## Pull Request Guidelines

- Keep PRs focused — one logical change per PR.
- Write a clear description of what changed and why.
- Reference any related issue (`Closes #123`).
- Do not commit secrets, generated files, or IDE configuration.

## Reporting Bugs

Open a GitHub issue using the bug report template.

## Security Issues

See [SECURITY.md](SECURITY.md) — do not open public issues for vulnerabilities.
