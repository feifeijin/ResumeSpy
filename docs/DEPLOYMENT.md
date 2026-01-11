# Deployment Implementation Guide

This guide covers how to implement actual deployment for the ResumeSpy backend API.

## Table of Contents
- [Current Status](#current-status)
- [Deployment Options](#deployment-options)
- [Option 1: Azure App Service](#option-1-azure-app-service)
- [Option 2: Railway](#option-2-railway)
- [Option 3: Docker + VPS](#option-3-docker--vps)
- [Option 4: AWS Elastic Beanstalk](#option-4-aws-elastic-beanstalk)
- [Database Deployment Options](#database-deployment-options)
- [Secrets Configuration](#secrets-configuration)
- [Next Steps](#next-steps)

## Current Status

The current CI/CD implementation includes:
- ✅ Automated build and test on pull requests
- ✅ Automated publish on merge to `main` or `release/**`
- ✅ Environment-specific workflows (DEV vs PROD)
- ⚠️ **Deployment stubs** (not actually deploying yet)

**What's Missing**: The actual deployment step that takes the published artifacts and deploys them to a hosting platform.

## Deployment Options

Choose a deployment strategy based on your requirements:

| Option | Best For | Cost | Complexity | .NET Support |
|--------|----------|------|------------|--------------|
| **Azure App Service** | Enterprise, Microsoft stack | $$$ | Low | Native |
| **Railway** | Startups, rapid deployment | $$ | Low | Docker |
| **Docker + VPS** | Full control, cost-effective | $ | Medium | Docker |
| **AWS Elastic Beanstalk** | AWS ecosystem | $$$ | Medium | Native |

## Option 1: Azure App Service

**Best for**: Teams already using Azure, enterprise deployments

### Prerequisites
1. Azure account and subscription
2. Azure CLI installed
3. Resource group created
4. App Service Plan created

### Setup Steps

#### 1. Create Azure Resources
```bash
# Login to Azure
az login

# Create resource group
az group create --name resumespy-rg --location eastus

# Create App Service Plan
az appservice plan create \
  --name resumespy-plan \
  --resource-group resumespy-rg \
  --sku B1 \
  --is-linux

# Create Web App (DEV)
az webapp create \
  --name resumespy-dev \
  --resource-group resumespy-rg \
  --plan resumespy-plan \
  --runtime "DOTNETCORE:9.0"

# Create Web App (PROD)
az webapp create \
  --name resumespy-prod \
  --resource-group resumespy-rg \
  --plan resumespy-plan \
  --runtime "DOTNETCORE:9.0"
```

#### 2. Configure Secrets in GitHub

Go to GitHub repo → Settings → Environments → DEV:
- `AZURE_WEBAPP_NAME`: `resumespy-dev`
- `AZURE_WEBAPP_PUBLISH_PROFILE`: (download from Azure Portal)

Repeat for PROD environment.

#### 3. Update `.github/workflows/cd.yml`

Replace the "Deploy to DEV" step:
```yaml
- name: Deploy to DEV
  if: github.ref == 'refs/heads/main'
  uses: azure/webapps-deploy@v2
  with:
    app-name: ${{ secrets.AZURE_WEBAPP_NAME }}
    publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
    package: ./publish
```

Replace the "Deploy to PROD" step:
```yaml
- name: Deploy to PROD
  if: startsWith(github.ref, 'refs/heads/release/')
  uses: azure/webapps-deploy@v2
  with:
    app-name: ${{ secrets.AZURE_WEBAPP_NAME }}
    publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
    package: ./publish
```

#### 4. Configure App Settings in Azure
```bash
# Configure DEV environment
az webapp config appsettings set \
  --name resumespy-dev \
  --resource-group resumespy-rg \
  --settings \
    ConnectionStrings__PrimaryDbConnection="${DEV_CONNECTION_STRING}" \
    Jwt__SigningKey="${DEV_JWT_SIGNING_KEY}" \
    ExternalAuth__Google__ClientId="${DEV_GOOGLE_CLIENT_ID}"

# Configure PROD environment
az webapp config appsettings set \
  --name resumespy-prod \
  --resource-group resumespy-rg \
  --settings \
    ConnectionStrings__PrimaryDbConnection="${PROD_CONNECTION_STRING}" \
    Jwt__SigningKey="${PROD_JWT_SIGNING_KEY}" \
    ExternalAuth__Google__ClientId="${PROD_GOOGLE_CLIENT_ID}"
```

### Monitoring
```bash
# View live logs
az webapp log tail --name resumespy-dev --resource-group resumespy-rg

# Enable Application Insights
az monitor app-insights component create \
  --app resumespy-insights \
  --location eastus \
  --resource-group resumespy-rg
```

## Option 2: Railway

**Best for**: Startups, quick deployment, minimal configuration

### Prerequisites
1. Railway account
2. Railway CLI installed
3. Dockerfile in repository

### Setup Steps

#### 1. Create Dockerfile
Create `Dockerfile` in repository root:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ResumeSpy.UI/ResumeSpy.UI.csproj", "ResumeSpy.UI/"]
COPY ["ResumeSpy.Infrastructure/ResumeSpy.Infrastructure.csproj", "ResumeSpy.Infrastructure/"]
COPY ["ResumeSpy.Core/ResumeSpy.Core.csproj", "ResumeSpy.Core/"]
RUN dotnet restore "ResumeSpy.UI/ResumeSpy.UI.csproj"
COPY . .
WORKDIR "/src/ResumeSpy.UI"
RUN dotnet build "ResumeSpy.UI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ResumeSpy.UI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ResumeSpy.UI.dll"]
```

#### 2. Configure Railway Project
```bash
# Login to Railway
railway login

# Create new project
railway init

# Link to existing project
railway link <project-id>
```

#### 3. Configure Secrets in GitHub

Go to GitHub repo → Settings → Environments → DEV:
- `RAILWAY_TOKEN`: (generate from Railway dashboard)
- `RAILWAY_SERVICE_DEV`: Service name in Railway (e.g., `resumespy-dev`)

#### 4. Update `.github/workflows/cd.yml`

Replace the "Deploy to DEV" step:
```yaml
- name: Deploy to DEV
  if: github.ref == 'refs/heads/main'
  run: |
    npm install -g @railway/cli
    railway up --service ${{ secrets.RAILWAY_SERVICE_DEV }}
  env:
    RAILWAY_TOKEN: ${{ secrets.RAILWAY_TOKEN }}
```

#### 5. Configure Environment Variables in Railway
In Railway dashboard, set:
- `ConnectionStrings__PrimaryDbConnection`
- `Jwt__SigningKey`
- `ExternalAuth__Google__ClientId`
- `ExternalAuth__Github__ClientId`
- `ExternalAuth__Github__ClientSecret`

### Database on Railway
Railway provides PostgreSQL databases:
```bash
# Add PostgreSQL to project
railway add --database postgresql

# Get connection string
railway variables
```

## Option 3: Docker + VPS

**Best for**: Cost-effective, full control, learning DevOps

### Prerequisites
1. VPS (DigitalOcean, Linode, AWS EC2, etc.)
2. Docker installed on VPS
3. SSH access configured
4. Domain name (optional)

### Setup Steps

#### 1. Create Dockerfile
Use the same Dockerfile from Railway option above.

#### 2. Set Up VPS
```bash
# SSH into VPS
ssh root@your-vps-ip

# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh

# Install Docker Compose
apt-get update
apt-get install -y docker-compose

# Create application directory
mkdir -p /opt/resumespy
```

#### 3. Create `docker-compose.yml` on VPS
```yaml
version: '3.8'

services:
  backend:
    image: ghcr.io/yourusername/resumespy:latest
    ports:
      - "8080:8080"
    environment:
      - ConnectionStrings__PrimaryDbConnection=Host=db;Port=5432;Database=resumespy;Username=postgres;Password=${POSTGRES_PASSWORD}
      - Jwt__SigningKey=${JWT_SIGNING_KEY}
      - ExternalAuth__Google__ClientId=${GOOGLE_CLIENT_ID}
    depends_on:
      - db
    restart: unless-stopped

  db:
    image: postgres:16
    environment:
      - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
      - POSTGRES_DB=resumespy
    volumes:
      - postgres_data:/var/lib/postgresql/data
    restart: unless-stopped

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ./ssl:/etc/nginx/ssl
    depends_on:
      - backend
    restart: unless-stopped

volumes:
  postgres_data:
```

#### 4. Configure GitHub Actions for Docker Deployment

Add to `.github/workflows/cd.yml` (before deploy step):
```yaml
- name: Build and Push Docker Image
  run: |
    echo ${{ secrets.GITHUB_TOKEN }} | docker login ghcr.io -u ${{ github.actor }} --password-stdin
    docker build -t ghcr.io/${{ github.repository_owner }}/resumespy:${{ github.sha }} .
    docker tag ghcr.io/${{ github.repository_owner }}/resumespy:${{ github.sha }} ghcr.io/${{ github.repository_owner }}/resumespy:latest
    docker push ghcr.io/${{ github.repository_owner }}/resumespy:${{ github.sha }}
    docker push ghcr.io/${{ github.repository_owner }}/resumespy:latest

- name: Deploy to VPS
  if: github.ref == 'refs/heads/main'
  uses: appleboy/ssh-action@master
  with:
    host: ${{ secrets.VPS_HOST }}
    username: ${{ secrets.VPS_USERNAME }}
    key: ${{ secrets.VPS_SSH_KEY }}
    script: |
      cd /opt/resumespy
      docker-compose pull backend
      docker-compose up -d backend
      docker system prune -f
```

#### 5. Configure Secrets in GitHub
- `VPS_HOST`: Your VPS IP address
- `VPS_USERNAME`: SSH username (e.g., `root`)
- `VPS_SSH_KEY`: Private SSH key for authentication

## Option 4: AWS Elastic Beanstalk

**Best for**: AWS-based infrastructure, auto-scaling needs

### Prerequisites
1. AWS account
2. AWS CLI configured
3. EB CLI installed

### Setup Steps

#### 1. Install EB CLI
```bash
pip install awsebcli
```

#### 2. Initialize Elastic Beanstalk
```bash
eb init -p "64bit Amazon Linux 2023 v3.0.2 running .NET 8" resumespy --region us-east-1
```

#### 3. Create Environments
```bash
# Create DEV environment
eb create resumespy-dev --database.engine postgres

# Create PROD environment
eb create resumespy-prod --database.engine postgres
```

#### 4. Update `.github/workflows/cd.yml`
```yaml
- name: Deploy to AWS Elastic Beanstalk (DEV)
  if: github.ref == 'refs/heads/main'
  run: |
    pip install awsebcli
    eb deploy resumespy-dev
  env:
    AWS_ACCESS_KEY_ID: ${{ secrets.AWS_ACCESS_KEY_ID }}
    AWS_SECRET_ACCESS_KEY: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
```

## Database Deployment Options

### Option 1: Managed Database (Recommended)
- **Azure Database for PostgreSQL**: Best with Azure App Service
- **AWS RDS PostgreSQL**: Best with AWS Elastic Beanstalk
- **Railway PostgreSQL**: Best with Railway deployment
- **DigitalOcean Managed Databases**: Good for VPS deployment

### Option 2: Self-Hosted Database
Use Docker Compose on VPS (see Option 3 above).

### Migration Strategy

#### Automatic Migrations (DEV only)
Add to CD workflow before deployment:
```yaml
- name: Apply Database Migrations (DEV)
  if: github.ref == 'refs/heads/main'
  run: |
    dotnet ef database update \
      --project ResumeSpy.Infrastructure \
      --startup-project ResumeSpy.UI \
      --connection "${{ secrets.DEV_CONNECTION_STRING }}"
```

#### Manual Migrations (PROD)
Generate SQL scripts and review before applying:
```bash
# Generate SQL script
dotnet ef migrations script \
  --project ResumeSpy.Infrastructure \
  --startup-project ResumeSpy.UI \
  --output migration.sql

# Review and apply manually
psql -h prod-db.com -U postgres -d resumespy -f migration.sql
```

## Secrets Configuration

### How to Configure GitHub Environment Secrets

1. Go to your GitHub repository
2. Navigate to: **Settings** → **Environments**
3. Create environments: `DEV` and `PROD`
4. For each environment, add secrets as shown below

### DEV Environment Secrets

| Secret Name | Value | Where to Get It |
|------------|-------|----------------|
| `DEV_DEPLOY_TOKEN` | Deployment authentication | Platform-specific (Azure/Railway/SSH key) |
| `DEV_CONNECTION_STRING` | PostgreSQL connection | Database provider dashboard |
| `DEV_JWT_SIGNING_KEY` | Random 32+ character string | `openssl rand -base64 32` |
| `DEV_GOOGLE_CLIENT_ID` | Google OAuth client ID | Google Cloud Console |
| `DEV_GITHUB_CLIENT_ID` | GitHub OAuth client ID | GitHub Developer Settings |
| `DEV_GITHUB_CLIENT_SECRET` | GitHub OAuth secret | GitHub Developer Settings |

### PROD Environment Secrets

Same as DEV, but with `PROD_` prefix and different values.

### Generating Secure Keys

```bash
# Generate JWT signing key
openssl rand -base64 64

# Generate random password
openssl rand -base64 32
```

## Next Steps

### Immediate Next Steps
1. **Choose Deployment Option**: Review options and select one
2. **Set Up Database**: Provision PostgreSQL instance
3. **Configure OAuth**: Set up Google/GitHub OAuth apps
4. **Configure Secrets**: Add all secrets to GitHub Environments
5. **Update CD Workflow**: Replace deployment stubs with actual deployment
6. **Test DEV Deployment**: Merge to `main` and verify deployment works
7. **Configure Monitoring**: Set up logging and alerting

### Recommended Implementation Order
1. ✅ Set up database (DEV)
2. ✅ Configure OAuth apps (DEV)
3. ✅ Add GitHub secrets (DEV)
4. ✅ Update CD workflow for DEV
5. ✅ Test DEV deployment
6. ✅ Repeat for PROD
7. ✅ Add health checks
8. ✅ Configure monitoring

### Optional Enhancements
- **Blue-Green Deployment**: Zero-downtime deployments
- **Canary Releases**: Gradual rollout to subset of users
- **Automated Rollback**: Rollback on health check failure
- **Performance Monitoring**: Application Insights / New Relic
- **Database Backups**: Automated backup and restore testing
- **Load Balancing**: Multiple instances for high availability

## Related Documentation

- [ENVIRONMENTS.md](./ENVIRONMENTS.md) - Environment configuration details
- [CONTRIBUTING.md](./CONTRIBUTING.md) - Contributing guidelines
- [README.md](../README.md) - Project overview
