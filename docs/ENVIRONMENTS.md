# Environment Configuration Guide

This guide documents the environment setup and configuration for the ResumeSpy backend API.

## Table of Contents
- [Environment Definitions](#environment-definitions)
- [Environment Configuration Matrix](#environment-configuration-matrix)
- [Required Secrets by Environment](#required-secrets-by-environment)
- [Database Configuration](#database-configuration)
- [CORS Configuration](#cors-configuration)
- [OAuth Configuration](#oauth-configuration)
- [Deployment Checklist](#deployment-checklist)
- [Security Best Practices](#security-best-practices)
- [Troubleshooting](#troubleshooting)

## Environment Definitions

ResumeSpy uses three distinct environments for development and deployment:

### Local Development (Local)
- **Purpose**: Development on your local machine
- **Database**: Local PostgreSQL instance
- **Deployment**: Manual (`dotnet run`)
- **Testing**: Full access to all features
- **Secrets**: User secrets or `.env` file (never committed)

### Development Environment (DEV)
- **Purpose**: Integration testing and staging
- **Database**: Shared DEV PostgreSQL instance
- **Deployment**: Automatic on merge to `main` branch
- **Testing**: Automated CI/CD validation
- **Secrets**: GitHub Environment secrets

### Production Environment (PROD)
- **Purpose**: Live production system
- **Database**: Production PostgreSQL with backups
- **Deployment**: Automatic on push to `release/**` branches
- **Testing**: Pre-deployment validation + post-deployment smoke tests
- **Secrets**: GitHub Environment secrets with protection rules

### Preview Environments (Not Applicable)
**Note**: Preview environments are **not applicable** for this backend API because:
- Backend APIs don't have PR-specific isolated instances (unlike frontend)
- Database state and migrations make ephemeral environments complex
- Testing should occur in DEV before release
- Frontend preview environments will call the DEV backend API

## Environment Configuration Matrix

| Configuration | Local | DEV | PROD |
|--------------|-------|-----|------|
| **Database** | `localhost:5432` | Hosted PostgreSQL (Azure/AWS/Railway) | Separate hosted PostgreSQL |
| **JWT Signing Key** | Development key | DEV-specific key (32+ chars) | PROD-specific key (64+ chars) |
| **Access Token Duration** | 60 minutes | 60 minutes | 15-30 minutes |
| **Refresh Token Duration** | 14 days | 7 days | 7 days |
| **Google OAuth** | Test app | DEV OAuth app | PROD OAuth app |
| **GitHub OAuth** | Test app | DEV OAuth app | PROD OAuth app |
| **CORS Origins** | `http://localhost:5173` | DEV frontend URL | PROD frontend URL |
| **Logging Level** | `Debug` | `Information` | `Warning` |
| **Telemetry** | Disabled | Enabled | Enabled + Alerts |

## Required Secrets by Environment

### Local Development
Configure via `appsettings.Development.json` or user secrets:

```json
{
  "ConnectionStrings": {
    "PrimaryDbConnection": "Host=localhost;Port=5432;Database=resumespy_dev;Username=postgres;Password=devpassword;"
  },
  "Jwt": {
    "Issuer": "ResumeSpy",
    "Audience": "ResumeSpyFrontend",
    "SigningKey": "your-local-development-signing-key-32-chars-minimum",
    "AccessTokenDurationInMinutes": 60,
    "RefreshTokenDurationInDays": 14
  },
  "ExternalAuth": {
    "Google": {
      "ClientId": "your-dev-google-client-id.apps.googleusercontent.com"
    },
    "Github": {
      "ClientId": "your-dev-github-client-id",
      "ClientSecret": "your-dev-github-client-secret"
    }
  }
}
```

### DEV Environment
Configure in GitHub repository settings → Environments → DEV:

| Secret Name | Description | Example |
|------------|-------------|---------|
| `DEV_DEPLOY_TOKEN` | Authentication token for deployment target | Azure SP credentials / Railway token |
| `DEV_CONNECTION_STRING` | PostgreSQL connection string | `Host=dev.postgres.com;Port=5432;...` |
| `DEV_JWT_SIGNING_KEY` | JWT signing key (32+ characters) | `dev-secret-key-32-characters-minimum-length` |
| `DEV_GOOGLE_CLIENT_ID` | Google OAuth client ID for DEV | `123456.apps.googleusercontent.com` |
| `DEV_GITHUB_CLIENT_ID` | GitHub OAuth client ID for DEV | `Iv1.abcd1234` |
| `DEV_GITHUB_CLIENT_SECRET` | GitHub OAuth client secret for DEV | `ghp_abc123...` |

### PROD Environment
Configure in GitHub repository settings → Environments → PROD:

| Secret Name | Description | Example |
|------------|-------------|---------|
| `PROD_DEPLOY_TOKEN` | Production deployment authentication | Azure SP credentials / Railway token |
| `PROD_CONNECTION_STRING` | Production PostgreSQL connection | `Host=prod.postgres.com;Port=5432;...` |
| `PROD_JWT_SIGNING_KEY` | Production JWT key (64+ characters) | `prod-ultra-secure-key-64-chars-min-random` |
| `PROD_GOOGLE_CLIENT_ID` | Google OAuth client ID for PROD | `789012.apps.googleusercontent.com` |
| `PROD_GITHUB_CLIENT_ID` | GitHub OAuth client ID for PROD | `Iv1.wxyz9876` |
| `PROD_GITHUB_CLIENT_SECRET` | GitHub OAuth client secret for PROD | `ghp_xyz789...` |

**IMPORTANT**: Enable environment protection rules for PROD:
- ✅ Required reviewers (at least 1)
- ✅ Wait timer (5-10 minutes)
- ✅ Restrict to specific branches (`release/**`)

## Database Configuration

### Local Database Setup
```bash
# Start PostgreSQL with Docker
docker run --name resumespy-postgres \
  -e POSTGRES_PASSWORD=devpassword \
  -e POSTGRES_DB=resumespy_dev \
  -p 5432:5432 \
  -d postgres:16

# Apply migrations
dotnet ef database update --project ResumeSpy.Infrastructure --startup-project ResumeSpy.UI
```

### DEV Database
- **Strategy**: Shared database for all DEV deployments
- **Migrations**: Automatically applied during deployment
- **Data**: Can be reset/seeded as needed
- **Backups**: Daily automated backups

### PROD Database
- **Strategy**: Dedicated production database
- **Migrations**: Applied with blue-green deployment or maintenance window
- **Data**: Critical user data, must be protected
- **Backups**: Hourly automated backups + point-in-time recovery

### Database Migration Strategy
```bash
# Generate migration
dotnet ef migrations add MigrationName \
  --project ResumeSpy.Infrastructure \
  --startup-project ResumeSpy.UI

# Apply migration (DEV)
dotnet ef database update \
  --project ResumeSpy.Infrastructure \
  --startup-project ResumeSpy.UI \
  --connection "Host=dev.db.com;..."

# Apply migration (PROD - use maintenance window)
dotnet ef database update \
  --project ResumeSpy.Infrastructure \
  --startup-project ResumeSpy.UI \
  --connection "Host=prod.db.com;..."
```

## CORS Configuration

CORS must be configured per environment to allow frontend access.

### Local
```json
{
  "AllowedOrigins": [
    "http://localhost:5173",
    "http://localhost:3000",
    "http://127.0.0.1:5173"
  ]
}
```

### DEV
```json
{
  "AllowedOrigins": [
    "https://dev.resumespy.com",
    "https://resumespy-dev.vercel.app"
  ]
}
```

### PROD
```json
{
  "AllowedOrigins": [
    "https://resumespy.com",
    "https://www.resumespy.com"
  ]
}
```

**Security Note**: Never use `*` (wildcard) in production CORS configuration.

## OAuth Configuration

### Google OAuth Setup
1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create separate OAuth apps for DEV and PROD
3. Configure authorized redirect URIs:
   - **DEV**: `https://dev.resumespy.com/auth/google/callback`
   - **PROD**: `https://resumespy.com/auth/google/callback`

### GitHub OAuth Setup
1. Go to GitHub Settings → Developer settings → OAuth Apps
2. Create separate OAuth apps for DEV and PROD
3. Configure callback URLs:
   - **DEV**: `https://dev.resumespy.com/auth/github/callback`
   - **PROD**: `https://resumespy.com/auth/github/callback`

**Frontend Integration**: Frontend is responsible for:
- Initiating OAuth flow
- Obtaining ID token (Google) or access token (GitHub)
- Calling backend `/api/auth/external` with the token

## Deployment Checklist

### Pre-Deployment (All Environments)
- [ ] Database migrations tested locally
- [ ] All secrets configured in GitHub Environments
- [ ] CORS origins verified
- [ ] OAuth apps configured with correct callback URLs
- [ ] Environment variables documented

### DEV Deployment
- [ ] Code merged to `main` branch
- [ ] CI workflow passed
- [ ] CD workflow triggered automatically
- [ ] Verify deployment logs
- [ ] Smoke test: Health check endpoint
- [ ] Smoke test: User authentication

### PROD Deployment
- [ ] Release branch created (`release/vX.Y.Z`)
- [ ] All features tested in DEV
- [ ] Database migration plan reviewed
- [ ] Manual approval obtained
- [ ] CD workflow triggered
- [ ] Deployment monitoring active
- [ ] Post-deployment smoke tests passed
- [ ] Rollback plan ready

## Security Best Practices

### Secrets Management
✅ **DO**:
- Use GitHub Environment secrets for all sensitive data
- Generate unique keys per environment
- Rotate JWT signing keys quarterly
- Use 64+ character keys for production
- Enable environment protection rules

❌ **DON'T**:
- Commit secrets to source control
- Reuse keys across environments
- Share production secrets in chat/email
- Use weak or guessable keys

### Database Security
✅ **DO**:
- Use connection pooling
- Enable SSL for database connections
- Restrict database access by IP
- Regular automated backups
- Test restore procedures

❌ **DON'T**:
- Expose database ports publicly
- Use default passwords
- Grant excessive permissions
- Skip migration testing

### API Security
✅ **DO**:
- Use HTTPS in all environments
- Implement rate limiting
- Log authentication failures
- Validate all inputs
- Use parameterized queries

❌ **DON'T**:
- Expose stack traces in production
- Allow unlimited login attempts
- Log sensitive data (passwords, tokens)
- Disable CSRF protection

## Troubleshooting

### Common Issues

#### Issue: "401 Unauthorized" in DEV
**Cause**: JWT signing key mismatch
**Solution**: 
1. Verify `DEV_JWT_SIGNING_KEY` is configured correctly
2. Check frontend is using the correct API base URL
3. Verify token is being sent in `Authorization: Bearer` header

#### Issue: "CORS policy blocked" error
**Cause**: Frontend origin not allowed
**Solution**:
1. Add frontend URL to `AllowedOrigins` configuration
2. Ensure HTTPS is used in production
3. Check for trailing slashes in URLs

#### Issue: Database connection timeout
**Cause**: Incorrect connection string or firewall
**Solution**:
1. Verify connection string format
2. Check database firewall allows GitHub Actions IP
3. Test connection from deployment environment

#### Issue: OAuth "redirect_uri_mismatch" error
**Cause**: OAuth callback URL not configured
**Solution**:
1. Update OAuth app configuration with correct callback URL
2. Ensure URL matches exactly (http vs https, trailing slash)
3. Verify OAuth app is using correct environment

### Health Check Endpoints

Use these endpoints to verify environment status:

```bash
# Health check
curl https://api.resumespy.com/health

# Database connectivity
curl https://api.resumespy.com/health/db

# Authentication service
curl https://api.resumespy.com/health/auth
```

### Logs and Monitoring

**DEV Environment**:
```bash
# View recent logs (example with Azure)
az webapp log tail --name resumespy-dev --resource-group resumespy

# Download logs
az webapp log download --name resumespy-dev --resource-group resumespy
```

**PROD Environment**:
- Use centralized logging (Application Insights, CloudWatch, etc.)
- Set up alerts for critical errors
- Monitor authentication failure rates
- Track API response times

## Next Steps

1. **Implement Real Deployment**: See [DEPLOYMENT.md](./DEPLOYMENT.md)
2. **Set Up Monitoring**: Configure Application Insights or CloudWatch
3. **Database Migrations**: Automate migration deployment
4. **Rollback Strategy**: Implement automated rollback on failure
5. **Performance Testing**: Load test DEV before PROD deployment

## Related Documentation

- [DEPLOYMENT.md](./DEPLOYMENT.md) - Deployment implementation guide
- [CONTRIBUTING.md](./CONTRIBUTING.md) - Contributing guidelines
- [README.md](../README.md) - Project overview and getting started
