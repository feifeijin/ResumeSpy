using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ResumeSpy.Core.Entities.Business.Auth;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.Infrastructure.Configuration;
using ResumeSpy.Infrastructure.Data;

namespace ResumeSpy.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthService> _logger;
        private readonly GoogleAuthSettings _googleSettings;
        private readonly GithubAuthSettings _githubSettings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _context;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            IOptions<ExternalAuthSettings> externalAuthOptions,
            ILogger<AuthService> logger,
            IHttpClientFactory httpClientFactory,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _context = context;

            var externalSettings = externalAuthOptions.Value ?? new ExternalAuthSettings();
            _googleSettings = externalSettings.Google;
            _githubSettings = externalSettings.Github;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return AuthResponse.Failed("This email address is already registered.");
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = true,
                DisplayName = request.DisplayName ?? request.Email,
                IsExternalLogin = false
            };

            var identityResult = await _userManager.CreateAsync(user, request.Password);
            if (!identityResult.Succeeded)
            {
                return AuthResponse.Failed(identityResult.Errors.Select(e => e.Description).ToArray());
            }

            var tokens = await _tokenService.GenerateTokenPairAsync(user, cancellationToken);

            return new AuthResponse
            {
                Succeeded = true,
                AccessToken = tokens.AccessToken,
                AccessTokenExpiresAt = tokens.AccessTokenExpiresAt,
                RefreshToken = tokens.RefreshToken,
                RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt,
                UserId = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                IsNewUser = true
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return AuthResponse.Failed("Invalid email or password.");
            }

            var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (signInResult.IsLockedOut)
            {
                return AuthResponse.Failed("Account locked. Please try again later.");
            }

            if (!signInResult.Succeeded)
            {
                return AuthResponse.Failed("Invalid email or password.");
            }

            var tokens = await _tokenService.GenerateTokenPairAsync(user, cancellationToken);

            return new AuthResponse
            {
                Succeeded = true,
                AccessToken = tokens.AccessToken,
                AccessTokenExpiresAt = tokens.AccessTokenExpiresAt,
                RefreshToken = tokens.RefreshToken,
                RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt,
                UserId = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                IsNewUser = false
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
        {
            var refreshResult = await _tokenService.RefreshTokenAsync(request, cancellationToken);
            if (!refreshResult.Succeeded || refreshResult.Tokens == null || refreshResult.User == null)
            {
                return AuthResponse.Failed(refreshResult.Errors);
            }

            return new AuthResponse
            {
                Succeeded = true,
                AccessToken = refreshResult.Tokens.AccessToken,
                AccessTokenExpiresAt = refreshResult.Tokens.AccessTokenExpiresAt,
                RefreshToken = refreshResult.Tokens.RefreshToken,
                RefreshTokenExpiresAt = refreshResult.Tokens.RefreshTokenExpiresAt,
                UserId = refreshResult.User.Id,
                Email = refreshResult.User.Email,
                DisplayName = refreshResult.User.DisplayName,
                IsNewUser = false
            };
        }

        public async Task LogoutAsync(string userId, LogoutRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return;
            }

            var refreshToken = await _context.UserRefreshTokens
                .Where(x => x.UserId == userId && x.Token == request.RefreshToken)
                .FirstOrDefaultAsync(cancellationToken);

            if (refreshToken == null)
            {
                return;
            }

            await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);
        }

        public async Task<AuthResponse> ExternalLoginAsync(ExternalAuthRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Provider))
            {
                return AuthResponse.Failed("Provider is required.");
            }

            var provider = request.Provider.Trim().ToLowerInvariant();
            return provider switch
            {
                ExternalProviders.Google => await HandleGoogleLoginAsync(request, cancellationToken),
                ExternalProviders.Github => await HandleGithubLoginAsync(request, cancellationToken),
                _ => AuthResponse.Failed("Unsupported external provider.")
            };
        }

        private async Task<AuthResponse> HandleGoogleLoginAsync(ExternalAuthRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.IdToken))
            {
                return AuthResponse.Failed("Google ID token is required.");
            }

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _googleSettings.ClientId }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to validate Google ID token");
                return AuthResponse.Failed("Invalid Google token.");
            }

            var providerKey = payload.Subject;
            var email = payload.Email ?? request.Email;

            if (string.IsNullOrWhiteSpace(email))
            {
                return AuthResponse.Failed("Unable to resolve email address from Google token.");
            }

            var loginInfo = new UserLoginInfo(ExternalProviders.Google, providerKey, "Google");

            var user = await _userManager.FindByLoginAsync(loginInfo.LoginProvider, loginInfo.ProviderKey);
            var isNewUser = false;

            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
                        DisplayName = payload.Name ?? request.DisplayName ?? email,
                        AvatarUrl = payload.Picture,
                        IsExternalLogin = true
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        return AuthResponse.Failed(createResult.Errors.Select(e => e.Description).ToArray());
                    }

                    isNewUser = true;
                }

                var addLoginResult = await _userManager.AddLoginAsync(user, loginInfo);
                if (!addLoginResult.Succeeded)
                {
                    return AuthResponse.Failed(addLoginResult.Errors.Select(e => e.Description).ToArray());
                }
            }

            var tokens = await _tokenService.GenerateTokenPairAsync(user, cancellationToken);

            return new AuthResponse
            {
                Succeeded = true,
                AccessToken = tokens.AccessToken,
                AccessTokenExpiresAt = tokens.AccessTokenExpiresAt,
                RefreshToken = tokens.RefreshToken,
                RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt,
                UserId = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                IsNewUser = isNewUser
            };
        }

        private async Task<AuthResponse> HandleGithubLoginAsync(ExternalAuthRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.AccessToken))
            {
                return AuthResponse.Failed("GitHub access token is required.");
            }

            using var httpClient = _httpClientFactory.CreateClient("github-oauth");
            httpClient.BaseAddress = new Uri("https://api.github.com/");
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ResumeSpy", "1.0"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken);

            var userResponse = await httpClient.GetAsync("user", cancellationToken);
            if (!userResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("GitHub user API returned status code {StatusCode}", userResponse.StatusCode);
                return AuthResponse.Failed("Invalid GitHub access token.");
            }

            var userContent = await userResponse.Content.ReadAsStringAsync(cancellationToken);
            var githubUser = JsonSerializer.Deserialize<GithubUserResponse>(userContent, JsonSerializerOptions);
            if (githubUser == null)
            {
                return AuthResponse.Failed("Failed to parse GitHub user response.");
            }

            var email = githubUser.Email;
            if (string.IsNullOrWhiteSpace(email))
            {
                var emailsResponse = await httpClient.GetAsync("user/emails", cancellationToken);
                if (emailsResponse.IsSuccessStatusCode)
                {
                    var emailsContent = await emailsResponse.Content.ReadAsStringAsync(cancellationToken);
                    var emailEntries = JsonSerializer.Deserialize<List<GithubEmailResponse>>(emailsContent, JsonSerializerOptions) ?? new();
                    email = emailEntries.FirstOrDefault(e => e.Primary && e.Verified)?.Email ?? emailEntries.FirstOrDefault()?.Email ?? request.Email;
                }
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                return AuthResponse.Failed("Unable to resolve email address from GitHub profile.");
            }

            var providerKey = githubUser.Id.ToString();
            var loginInfo = new UserLoginInfo(ExternalProviders.Github, providerKey, "GitHub");

            var user = await _userManager.FindByLoginAsync(loginInfo.LoginProvider, loginInfo.ProviderKey);
            var isNewUser = false;

            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
                        DisplayName = githubUser.Name ?? request.DisplayName ?? email,
                        AvatarUrl = githubUser.AvatarUrl,
                        IsExternalLogin = true
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        return AuthResponse.Failed(createResult.Errors.Select(e => e.Description).ToArray());
                    }

                    isNewUser = true;
                }

                var addLoginResult = await _userManager.AddLoginAsync(user, loginInfo);
                if (!addLoginResult.Succeeded)
                {
                    return AuthResponse.Failed(addLoginResult.Errors.Select(e => e.Description).ToArray());
                }
            }

            var tokens = await _tokenService.GenerateTokenPairAsync(user, cancellationToken);

            return new AuthResponse
            {
                Succeeded = true,
                AccessToken = tokens.AccessToken,
                AccessTokenExpiresAt = tokens.AccessTokenExpiresAt,
                RefreshToken = tokens.RefreshToken,
                RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt,
                UserId = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                IsNewUser = isNewUser
            };
        }

        private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

        private sealed record GithubUserResponse(
            [property: JsonPropertyName("id")] long Id,
            [property: JsonPropertyName("login")] string? Login,
            [property: JsonPropertyName("name")] string? Name,
            [property: JsonPropertyName("email")] string? Email,
            [property: JsonPropertyName("avatar_url")] string? AvatarUrl);

        private sealed record GithubEmailResponse(
            [property: JsonPropertyName("email")] string Email,
            [property: JsonPropertyName("primary")] bool Primary,
            [property: JsonPropertyName("verified")] bool Verified,
            [property: JsonPropertyName("visibility")] string? Visibility);
    }
}
