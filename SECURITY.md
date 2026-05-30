# Security Policy

## Supported Versions

Only the latest release on the `main` branch receives security fixes. Older
releases are not maintained.

## Reporting a Vulnerability

If you believe you have found a security vulnerability in ResumeSpy, please
report it privately. **Do not open a public GitHub issue.**

Preferred channels:

- **GitHub Security Advisories**: Use the
  [private vulnerability reporting](https://github.com/feifeijin/ResumeSpy/security/advisories/new)
  form on this repository.

When reporting, please include:

- A clear description of the issue and the impact you observed.
- Steps to reproduce (proof-of-concept, request payloads, affected endpoint).
- The commit SHA or release version you tested against.
- Any suggested remediation, if you have one.

## Response Expectations

- We aim to acknowledge new reports within **5 business days**.
- We aim to provide an initial assessment within **10 business days**.
- Fix timelines depend on severity; high-impact issues are prioritized.

We will credit reporters in the release notes unless you request otherwise.

## Out of Scope

The following are generally considered out of scope:

- Vulnerabilities in third-party services (Render, Supabase, Vercel, GitHub).
- Self-XSS or social-engineering attacks.
- Findings from automated scanners without a working proof-of-concept.
- Denial-of-service via volumetric traffic.

## Hardening Notes for Operators

If you self-host ResumeSpy, please review:

- Keep `Jwt:SigningKey` and OAuth client secrets in a secret manager or
  environment variables — never commit them.
- Restrict CORS origins to the domains you actually serve (see
  `Cors:AllowedOrigins` in `appsettings.json`).
- Run behind HTTPS in all non-local environments.
- Apply database migrations with least-privileged credentials.
