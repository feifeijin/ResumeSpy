# Security Policy

## Supported Versions

| Version | Supported |
|---------|-----------|
| Latest (`main`) | Yes |

## Reporting a Vulnerability

**Do not open a public GitHub issue for security vulnerabilities.**

Please report security issues by emailing the maintainer directly. Include:

- A clear description of the vulnerability
- Steps to reproduce (proof-of-concept if possible)
- The potential impact
- Any suggested remediation

You can expect an acknowledgement within 72 hours and a fix timeline within 14 days for critical issues.

## Security Design

- **Authentication**: Delegated to [Supabase](https://supabase.com) — JWTs are validated against the Supabase OIDC/JWKS endpoint. The backend does not issue or store tokens.
- **Secrets**: Never committed to source control. Injected at runtime via environment variables.
- **Database**: Uses parameterized queries via Entity Framework Core. No raw SQL with user input.
- **Headers**: Security headers middleware enforces HSTS, X-Frame-Options, X-Content-Type-Options, and Content-Security-Policy.
- **Logging**: Sensitive headers (`Authorization`, `Cookie`, `Set-Cookie`, `X-API-Key`, `X-Auth-Token`) are redacted from request/response logs.
- **Rate limiting**: Applied at the API layer.
- **CORS**: Configured via `Cors:AllowedOrigins` — never a wildcard in production.

## Known Limitations

- The `AutoMapper` package (12.0.1) has a known upstream vulnerability (NU1903). This is tracked and will be remediated when a compatible upgrade is available.
