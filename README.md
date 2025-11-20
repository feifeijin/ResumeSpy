# ResumeSpy

ResumeSpy is a back-end application designed to efficiently manage resumes with version control, multi-language support, and customization for specific job descriptions (JDs).

## Features
- **Single Source Maintenance**: Manage one primary language version for each resume position.
- **Version Control**: Integrated with Git to track changes and compare versions.
- **Multi-language Support**: Support for translating resumes to various languages.
- **JD-specific Customization**: Allows resume creation tailored for job descriptions.
- **User Authentication**: JWT-based API authentication with refresh tokens plus Google and GitHub social login support.

## Technology Stack

- **Back-End**: .NET Core Web API
- **Database**: TBD
- **Version Control**: Git for tracking resume versions
- **Translation API**: TBD

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
