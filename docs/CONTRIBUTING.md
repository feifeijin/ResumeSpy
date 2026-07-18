# Contributing to ResumeSpy

Thank you for your interest in contributing to ResumeSpy! This guide will help you get started.

## Table of Contents
- [Getting Started](#getting-started)
- [Development Workflow](#development-workflow)
- [Branch Naming Conventions](#branch-naming-conventions)
- [Commit Message Format](#commit-message-format)
- [Code Standards and Conventions](#code-standards-and-conventions)
- [Testing Guidelines](#testing-guidelines)
- [Pull Request Process](#pull-request-process)
- [Areas for Contribution](#areas-for-contribution)

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- PostgreSQL 16+ (or Docker)
- Git
- Your favorite IDE (Visual Studio, VS Code, Rider)

### Local Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/feifeijin/ResumeSpy.git
   cd ResumeSpy
   ```

2. **Set up PostgreSQL database**
   ```bash
   # Using Docker (recommended)
   docker run --name resumespy-postgres \
     -e POSTGRES_PASSWORD=devpassword \
     -e POSTGRES_DB=resumespy_dev \
     -p 5432:5432 \
     -d postgres:16
   ```

3. **Configure user secrets**
   ```bash
   # Navigate to UI project
   cd ResumeSpy.UI

   # Initialize user secrets
   dotnet user-secrets init

   # Set connection string
   dotnet user-secrets set "ConnectionStrings:PrimaryDbConnection" "Host=localhost;Port=5432;Database=resumespy_dev;Username=postgres;Password=devpassword;"

   # Set JWT configuration
   dotnet user-secrets set "Jwt:SigningKey" "your-local-dev-key-32-chars-minimum"
   ```

4. **Apply database migrations**
   ```bash
   dotnet ef database update \
     --project ResumeSpy.Infrastructure \
     --startup-project ResumeSpy.UI
   ```

5. **Restore dependencies and build**
   ```bash
   cd ..
   dotnet restore ResumeSpy.sln
   dotnet build ResumeSpy.sln --configuration Debug
   ```

6. **Run the API**
   ```bash
   dotnet run --project ResumeSpy.UI
   ```

7. **Test the API**
   Open `https://localhost:7227/swagger.html` in your browser

## Development Workflow

We use a feature branch workflow with automated CI/CD:

```
main (protected)
  ↑
  └─ feature/your-feature-name
  └─ bugfix/issue-description
  └─ chore/task-description

release/** (protected)
  ↑
  └─ release/v1.0.0
```

### Standard Workflow

1. **Create a feature branch**
   ```bash
   git checkout main
   git pull origin main
   git checkout -b feature/add-resume-export
   ```

2. **Make your changes**
   - Write code
   - Add tests (if applicable)
   - Update documentation

3. **Test locally**
   ```bash
   # Build
   dotnet build ResumeSpy.sln --configuration Release

   # Run tests
   dotnet test ResumeSpy.sln --configuration Release

   # Run the API and verify
   dotnet run --project ResumeSpy.UI
   ```

4. **Commit your changes**
   ```bash
   git add .
   git commit -m "feat: add PDF export for resumes"
   ```

5. **Push and create PR**
   ```bash
   git push origin feature/add-resume-export
   ```
   Then create a pull request on GitHub targeting `main`.

6. **CI/CD automatically runs**
   - ✅ Build validation
   - ✅ Test execution
   - ✅ Code quality checks

7. **Review and merge**
   - Address review comments
   - Once approved, merge to `main`
   - DEV deployment happens automatically

## Branch Naming Conventions

Use these prefixes for branch names:

| Prefix | Purpose | Example |
|--------|---------|---------|
| `feature/` | New features | `feature/add-resume-templates` |
| `bugfix/` | Bug fixes | `bugfix/fix-auth-token-refresh` |
| `hotfix/` | Urgent production fixes | `hotfix/security-patch` |
| `chore/` | Maintenance, refactoring | `chore/update-dependencies` |
| `docs/` | Documentation only | `docs/update-api-guide` |
| `test/` | Test additions/fixes | `test/add-auth-integration-tests` |
| `release/` | Release branches | `release/v1.2.0` |

### Examples
```bash
# Good branch names
feature/resume-version-history
bugfix/cors-configuration-error
chore/update-dotnet-packages
docs/add-deployment-guide

# Bad branch names
my-changes
fix
update
john-dev-branch
```

## Commit Message Format

We follow the [Conventional Commits](https://www.conventionalcommits.org/) specification:

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, no logic change)
- `refactor`: Code refactoring
- `perf`: Performance improvements
- `test`: Adding or updating tests
- `chore`: Maintenance tasks
- `ci`: CI/CD changes

### Examples

```bash
# Simple feature
git commit -m "feat: add resume PDF export endpoint"

# Bug fix with scope
git commit -m "fix(auth): resolve token refresh race condition"

# Breaking change
git commit -m "feat!: change authentication to OAuth2 only

BREAKING CHANGE: Email/password authentication removed.
Users must now authenticate via Google or GitHub."

# Multiple changes
git commit -m "feat: add resume version history

- Add ResumeVersion entity
- Implement version tracking service
- Add API endpoints for version retrieval
- Update database schema

Closes #123"
```

### Commit Message Guidelines
- ✅ Use present tense ("add feature" not "added feature")
- ✅ Use imperative mood ("move cursor to..." not "moves cursor to...")
- ✅ Keep subject line under 72 characters
- ✅ Reference issues and PRs where applicable
- ❌ Don't end subject line with a period
- ❌ Don't include personal notes or WIP markers

## Code Standards and Conventions

### C# Coding Standards

Follow the conventions established in the codebase:

```csharp
// ✅ Good: Follow existing naming conventions
public class ResumeService : IResumeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ResumeService> _logger;

    public async Task<Result<ResumeDto>> GetResumeAsync(string id)
    {
        // Implementation
    }
}

// ❌ Bad: Different conventions
public class resumeService : IResumeService
{
    private IUnitOfWork unitOfWork;
    
    public Result<ResumeDto> GetResume(string id)
    {
        // Synchronous when should be async
    }
}
```

### Architectural Patterns

ResumeSpy follows Clean Architecture:

```
ResumeSpy.Core (Domain Layer)
  └─ Entities/         # Domain entities
  └─ Interfaces/       # Repository and service interfaces
  └─ Services/         # Business logic services

ResumeSpy.Infrastructure (Infrastructure Layer)
  └─ Repositories/     # Data access implementations
  └─ Services/         # External service integrations
      └─ AI/           # AI provider implementations
      └─ Translation/  # Translation services

ResumeSpy.UI (Presentation Layer)
  └─ Controllers/      # API controllers
  └─ Models/           # API DTOs
  └─ Middleware/       # Custom middleware
```

### Key Conventions

1. **Async/Await**: All I/O operations must be async
   ```csharp
   // ✅ Good
   public async Task<Resume> GetResumeAsync(string id)
   {
       return await _repository.GetByIdAsync(id);
   }

   // ❌ Bad
   public Resume GetResume(string id)
   {
       return _repository.GetById(id);
   }
   ```

2. **Dependency Injection**: Use constructor injection
   ```csharp
   // ✅ Good
   public class ResumeController : ControllerBase
   {
       private readonly IResumeService _resumeService;

       public ResumeController(IResumeService resumeService)
       {
           _resumeService = resumeService;
       }
   }
   ```

3. **Repository Pattern**: All data access through repositories
   ```csharp
   // ✅ Good
   var resume = await _unitOfWork.Resumes.GetByIdAsync(id);

   // ❌ Bad
   var resume = await _dbContext.Resumes.FindAsync(id);
   ```

4. **Error Handling**: Use Result pattern or throw specific exceptions
   ```csharp
   // ✅ Good
   if (resume == null)
   {
       return NotFound(new { message = "Resume not found" });
   }

   // ❌ Bad
   if (resume == null)
   {
       throw new Exception("Error");
   }
   ```

## Testing Guidelines

### Test Structure (When Tests Exist)

Currently, ResumeSpy does not have a test project, but when adding tests:

```
ResumeSpy.Tests/
  ├─ Unit/
  │   ├─ Services/
  │   └─ Controllers/
  ├─ Integration/
  │   └─ Api/
  └─ Fixtures/
```

### Writing Tests

```csharp
public class ResumeServiceTests
{
    [Fact]
    public async Task GetResumeAsync_ExistingId_ReturnsResume()
    {
        // Arrange
        var mockRepo = new Mock<IResumeRepository>();
        var service = new ResumeService(mockRepo.Object);

        // Act
        var result = await service.GetResumeAsync("test-id");

        // Assert
        Assert.NotNull(result);
    }
}
```

### Test Naming Convention
```
MethodName_Scenario_ExpectedBehavior
```

Examples:
- `GetResumeAsync_ExistingId_ReturnsResume`
- `CreateResumeAsync_InvalidData_ThrowsValidationException`
- `UpdateResumeAsync_NonExistentId_ReturnsNotFound`

## Pull Request Process

### Before Creating a PR

- [ ] Code builds successfully
- [ ] All tests pass (if tests exist)
- [ ] Code follows established conventions
- [ ] Documentation is updated (if applicable)
- [ ] Commits follow conventional commit format
- [ ] Branch is up to date with `main`

### Creating a PR

1. **Use a descriptive title**
   ```
   Good: "feat: Add PDF export functionality for resumes"
   Bad: "Update files"
   ```

2. **Fill out the PR template**
   ```markdown
   ## Description
   Brief description of changes

   ## Type of Change
   - [ ] Bug fix
   - [x] New feature
   - [ ] Breaking change
   - [ ] Documentation update

   ## Testing
   - Tested locally with PostgreSQL
   - Verified API endpoints with Swagger
   - Added unit tests for new service methods

   ## Checklist
   - [x] Code follows project conventions
   - [x] Documentation updated
   - [x] Tests added/updated
   ```

3. **Link related issues**
   ```markdown
   Closes #123
   Fixes #456
   Related to #789
   ```

### PR Review Process

1. **Automated CI runs**
   - Build validation
   - Test execution
   - Code quality checks

2. **Code review** (1+ approvals required)
   - Maintainer reviews code
   - Automated feedback provided
   - Discussions resolved

3. **Merge**
   - Squash and merge (preferred for features)
   - Merge commit (for release branches)
   - Rebase and merge (for small fixes)

### After Merge

- ✅ CI/CD automatically deploys to DEV
- ✅ Branch is automatically deleted
- ✅ Issue is automatically closed (if linked)

## Areas for Contribution

### High Priority

1. **Testing Infrastructure**
   - Set up test project
   - Add unit tests for services
   - Add integration tests for API endpoints

2. **API Features**
   - Resume templates
   - Resume version history
   - Resume comparison tool
   - Export to multiple formats (DOCX, JSON)

3. **AI/Translation Improvements**
   - Additional AI providers
   - Translation quality metrics
   - Caching optimizations

### Medium Priority

4. **Documentation**
   - API documentation improvements
   - Code examples
   - Tutorial videos

5. **DevOps**
   - Docker containerization
   - Real deployment implementation
   - Monitoring and logging

6. **Security**
   - Rate limiting
   - Audit logging
   - Security headers

### Low Priority (Nice to Have)

7. **Performance**
   - Query optimization
   - Response caching
   - CDN integration

8. **User Experience**
   - Webhook support
   - Batch operations
   - Search and filtering improvements

## Getting Help

- **Questions**: Open a GitHub Discussion
- **Bugs**: Create an issue with `bug` label
- **Features**: Create an issue with `enhancement` label
- **Security**: Email security@resumespy.com (private disclosure)

## Code of Conduct

- Be respectful and inclusive
- Provide constructive feedback
- Focus on the code, not the person
- Help others learn and grow

## License

By contributing, you agree that your contributions will be licensed under the same license as the project.

## Related Documentation

- [README.md](../README.md) - Project overview
- [ENVIRONMENTS.md](./ENVIRONMENTS.md) - Environment setup
- [DEPLOYMENT.md](./DEPLOYMENT.md) - Deployment guide
