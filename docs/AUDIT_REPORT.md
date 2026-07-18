# GitHub Open-Source Readiness & Information Leakage Audit

_Audit date: 2026-05-30_
_Scope: backend repository at this directory (`/backend`)_

This report documents the findings of a pre-publication audit covering
sensitive-information leakage, documentation review, open-source readiness,
`.gitignore` review, personal-brand risk, and competitive exposure. Where a
finding has already been remediated in the same change as this report, it is
marked **(Fixed in this PR)**. Where it requires action by the repository
owner, it is marked **(Action required)**.

---

## 1. Sensitive Information Leakage Review

### Findings

| # | Location | Issue | Risk | Status |
| --- | --- | --- | --- | --- |
| 1 | `ResumeSpy.UI/Program.cs:153` (original) | Hardcoded personal subdomain `resumespy.feifeijin.com` in CORS allowlist. | **Medium** — ties the codebase to the maintainer's personal domain and exposes internal infra to scrapers. | Fixed in this PR (moved to `Cors:AllowedOrigins`) |
| 2 | `ResumeSpy.UI/Program.cs:157` (original) | Hardcoded Vercel preview suffix `-feifeijins-projects.vercel.app`. | **Medium** — leaks the maintainer's Vercel account namespace. | Fixed in this PR (moved to `Cors:AllowedHostSuffixes`) |
| 3 | `ResumeSpy.UI/Program.cs:149` (original) | Hardcoded production frontend domain `resume-spy-web.vercel.app`. | **Low** — production URL discoverable but not a credential. | Fixed in this PR (moved to `Cors:AllowedOrigins`) |
| 4 | `docs/ENVIRONMENTS.md` (original) | Real production domains exposed: `resumespy.com`, `dev.resumespy.com`, `api.resumespy.com`, `resumespy-dev.vercel.app`. | **Low–Medium** — gives attackers a target list and reveals environment layout. | Fixed in this PR (replaced with `example.com` / `example.app`) |
| 5 | `docs/CONTRIBUTING.md` (original) | `security@resumespy.com` referenced but `SECURITY.md` did not exist; mailbox status unknown. | **Low** — broken security-disclosure path is itself a risk. | Fixed in this PR (replaced with `SECURITY.md` link) |
| 6 | `.github/workflows/claude-execute.yml:41` | `bot@resumespy.dev` git-commit identity used by automation. | **Low** — likely placeholder; verify domain ownership or use a `noreply` form (e.g. `bot@users.noreply.github.com`). | Action required |
| 7 | `docker-compose.yml` | Local-dev Postgres uses `POSTGRES_PASSWORD: postgres`. | **Low** — clearly local-dev only, but readers may copy into production. Consider adding a comment noting it is local only. | Action recommended |
| 8 | `.env.example` | Default SMTP `localhost:1025` (MailHog). | **None** — standard local-dev pattern. | OK |
| 9 | `appsettings.json` | All secret fields are blank strings. | **None** — verified no real keys committed. | OK |
| 10 | Repository-wide grep for `sk-`, `ghp_`, `AKIA`, `AIza`, `eyJ...`, etc. | No real API keys or tokens were found in source. | **None** | OK |

### What was checked

The following were inspected for accidental secret leakage and personal-data
exposure:

- All files at repository root (including hidden files).
- `.github/workflows/*.yml` (3 workflows).
- `.github/copilot-instructions.md`, `.github/ISSUE_TEMPLATE/*`.
- `appsettings.json`, `Properties/launchSettings.json`, `*.http` files.
- `Dockerfile`, `docker-compose.yml`, `.dockerignore`.
- `.env.example`, `.gitignore`.
- All files under `docs/`.
- `Program.cs` (CORS logic).
- The `.deprecated/` directory (8 legacy `GuestSession*` files).
- Repository-wide regex sweeps for common token prefixes and PII patterns.

---

## 2. Documentation Review (`/docs`)

| File | Public Safe? | Risk Level | Reason |
| --- | --- | --- | --- |
| `docs/CONTRIBUTING.md` | ✅ Yes | Low | Standard contributor guide. Security email reference fixed in this PR. |
| `docs/DEPLOYMENT.md` | ✅ Yes | Low | Mentions Render and Vercel as the current hosts, which is fine for a public OSS project. Generic platform setup guidance. |
| `docs/ENVIRONMENTS.md` | ✅ Yes (after redaction) | Was Medium → Low | Originally leaked real production domains. Redacted in this PR. |
| `docs/AI_TRANSLATION_IMPLEMENTATION.md` | ✅ Yes (after rewrite) | Was Medium → Low | Originally written as a first-person internal status note ("I've successfully implemented..."), which read as unfinished/AI-generated. Rewritten as developer documentation in this PR. |
| `docs/AUDIT_REPORT.md` (this file) | ✅ Yes | Low | Intentional public artifact for transparency. |

There are no roadmaps, growth plans, SEO strategies, revenue plans, or
competitive analyses checked in to `docs/`. The README's roadmap section is
generic engineering milestones, not market strategy.

---

## 3. Repository Open-Source Readiness

### Before this PR

| Item | Status |
| --- | --- |
| `README.md` | Present |
| `LICENSE` | **Missing** — README claimed MIT but no file existed. |
| `SECURITY.md` | **Missing** |
| `CODE_OF_CONDUCT.md` | **Missing** |
| `CONTRIBUTING.md` | Present (under `docs/`) |
| PR template | **Missing** |
| Issue templates | Present (`bug_report.md`, `feature_request.md`) |
| `.deprecated/` legacy code | Committed at repo root |
| .NET version drift | README says .NET 9; CI uses .NET 10. |

### Fixed in this PR

- Added `LICENSE` (MIT, "ResumeSpy Contributors").
- Added `SECURITY.md` with private-disclosure flow.
- Added `CODE_OF_CONDUCT.md` (Contributor Covenant 2.1).
- Added `.github/pull_request_template.md`.
- Deleted `.deprecated/` (8 `GuestSession*` files). History remains in git.

### Action required (not in this PR)

- Reconcile .NET version drift between `README.md` (claims 9.0) and the CI/CD
  workflows (target 10.0). The Dockerfile also uses `dotnet/aspnet:10.0`.
- Confirm the `bot@resumespy.dev` identity in `claude-execute.yml` or switch
  to a `noreply` form.

---

## 4. `.gitignore` Review

### Before this PR

The original `.gitignore` covered the essentials (`bin/`, `obj/`, `.env`,
`appsettings.Development.json`) but was missing common entries that could
cause accidental leakage from contributors with different editors or runners.

### Fixed in this PR

- Added IDE/editor entries: `.idea/`, `.vs/`, `*.user`, `*.suo`,
  `*.userprefs`, `*.swp`, `*.swo`, `.DS_Store`, `Thumbs.db`.
- Added secrets safety: `secrets.json` (dotnet user-secrets export),
  `appsettings.*.local.json`, `.env.production`.
- Added test/coverage outputs: `TestResults/`, `coverage/`, `*.coverage`,
  `*.coveragexml`.
- Added log paths: `*.log`, `logs/`.
- Kept the `!.env.example` allowlist so the template stays tracked.

---

## 5. Personal Brand Risk Review

| # | Finding | Recommendation | Status |
| --- | --- | --- | --- |
| 1 | Hardcoded personal Vercel namespace and personal subdomain in source. | Move to config (done) and prefer environment-specific config in deploy targets. | Fixed in this PR |
| 2 | `AI_TRANSLATION_IMPLEMENTATION.md` written in first person ("I've successfully implemented...") — reads as an internal AI-assisted status note rather than reference documentation. | Rewrite as third-person reference documentation. | Fixed in this PR |
| 3 | README "Limitations" section is candid (no tests, no monitoring, no rollback). | This is honest and acceptable for a portfolio project. Consider tightening once Phase 2 lands. | No action needed |
| 4 | `bot@resumespy.dev` email may not resolve. | Verify or replace with a `noreply` GitHub address. | Action required |

The project does not contain personal notes, WIP markers, or AI-generated
"plan" documents that should be excluded from publication.

---

## 6. Competitive Exposure Review

The repository does not contain:

- Growth strategies or marketing plans.
- SEO strategies.
- Revenue or monetization plans.
- Competitive analyses.
- Viral mechanics or unique market positioning beyond the public README.

The technical roadmap in `README.md` ("Phase 2: Deployment", "Phase 3: Testing",
etc.) describes engineering maturity steps, not business strategy. Publishing
it does not create a competitive disadvantage.

**Recommendation**: No business content needs to be moved to a private
repository. If product/business planning starts to accumulate, keep it in a
separate private repo or wiki rather than in `docs/`.

---

## 7. Final Verdict

### Scores (after this PR is merged)

| Category | Score (0–100) |
| --- | --- |
| **Security** | **90** — No leaked secrets. Two residual items (`bot@resumespy.dev`, Docker dev password comment) are low risk. |
| **Open-Source Readiness** | **88** — LICENSE, SECURITY.md, CODE_OF_CONDUCT.md, PR template, issue templates, README, CONTRIBUTING all present. .NET version drift between README and CI is the main gap. |
| **Personal Brand** | **85** — Documentation cleaned up, hardcoded personal infra removed. The repo still ties back to the maintainer via the GitHub username, which is expected for a portfolio project. |

### Publish recommendation

**Publish after minor cleanup** — the residual action items are low risk and
can be addressed in follow-up PRs without blocking publication:

1. Resolve the .NET 9 vs .NET 10 documentation drift.
2. Verify or replace the `bot@resumespy.dev` automation email.
3. Add a comment in `docker-compose.yml` noting that the Postgres password is
   for local development only.

Once those are done, the repository is in good shape for public release.
