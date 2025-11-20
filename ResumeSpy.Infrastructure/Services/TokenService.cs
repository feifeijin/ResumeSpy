using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ResumeSpy.Core.Entities.Business.Auth;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.Infrastructure.Configuration;
using ResumeSpy.Infrastructure.Data;

namespace ResumeSpy.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly ApplicationDbContext _context;
        private readonly TokenValidationParameters _tokenValidationParameters;

        public TokenService(
            IOptions<JwtSettings> jwtOptions,
            ApplicationDbContext context,
            TokenValidationParameters tokenValidationParameters)
        {
            _jwtSettings = jwtOptions.Value;
            _context = context;
            _tokenValidationParameters = tokenValidationParameters;
        }

        public async Task<TokenResult> GenerateTokenPairAsync(ApplicationUser user, CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.DisplayName ?? user.Email ?? user.Id)
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SigningKey));
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = utcNow.AddMinutes(_jwtSettings.AccessTokenDurationInMinutes),
                SigningCredentials = signingCredentials,
                Audience = _jwtSettings.Audience,
                Issuer = _jwtSettings.Issuer
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var jwt = tokenHandler.WriteToken(securityToken);

            var refreshToken = new UserRefreshToken
            {
                Id = Guid.NewGuid().ToString(),
                UserId = user.Id,
                Token = GenerateSecureRefreshToken(),
                JwtId = securityToken.Id,
                IsRevoked = false,
                IsUsed = false,
                CreatedAt = utcNow,
                ExpiresAt = utcNow.AddDays(_jwtSettings.RefreshTokenDurationInDays),
                EntryDate = utcNow,
                UpdateDate = utcNow
            };

            _context.UserRefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync(cancellationToken);

            return new TokenResult
            {
                AccessToken = jwt,
                AccessTokenExpiresAt = tokenDescriptor.Expires ?? utcNow,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt,
                RefreshTokenId = refreshToken.Id
            };
        }

        public async Task<TokenRefreshResult> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
        {
            var principal = GetPrincipalFromToken(request.AccessToken, ignoreExpiry: true, out var securityToken, out var validationError);
            if (principal == null || securityToken == null)
            {
                return TokenRefreshResult.Failed(validationError ?? "Invalid access token supplied.");
            }

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
            {
                return TokenRefreshResult.Failed("Invalid token algorithm.");
            }

            var storedRefreshToken = await _context.UserRefreshTokens
                .FirstOrDefaultAsync(x => x.Token == request.RefreshToken, cancellationToken);

            if (storedRefreshToken == null)
            {
                return TokenRefreshResult.Failed("Refresh token does not exist.");
            }

            if (storedRefreshToken.IsRevoked)
            {
                return TokenRefreshResult.Failed("Refresh token has been revoked.");
            }

            if (storedRefreshToken.IsUsed)
            {
                return TokenRefreshResult.Failed("Refresh token has already been used.");
            }

            if (storedRefreshToken.ExpiresAt <= DateTime.UtcNow)
            {
                return TokenRefreshResult.Failed("Refresh token has expired.");
            }

            var jti = principal.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
            if (jti == null || storedRefreshToken.JwtId != jti)
            {
                return TokenRefreshResult.Failed("Token identifiers do not match.");
            }

            storedRefreshToken.IsUsed = true;
            storedRefreshToken.UpdateDate = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return TokenRefreshResult.Failed("User identifier not present in token.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null)
            {
                return TokenRefreshResult.Failed("User not found.");
            }

            var newTokens = await GenerateTokenPairAsync(user, cancellationToken);

            return new TokenRefreshResult
            {
                Succeeded = true,
                Tokens = newTokens,
                User = user
            };
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            var token = await _context.UserRefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshToken, cancellationToken);
            if (token == null)
            {
                return;
            }

            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.UpdateDate = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
        }

        private ClaimsPrincipal? GetPrincipalFromToken(string token, bool ignoreExpiry, out SecurityToken? securityToken, out string? error)
        {
            securityToken = null;
            error = null;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = _tokenValidationParameters.Clone();
                validationParameters.ValidateLifetime = !ignoreExpiry;

                var principal = tokenHandler.ValidateToken(token, validationParameters, out securityToken);
                return principal;
            }
            catch (SecurityTokenExpiredException)
            {
                if (!ignoreExpiry)
                {
                    error = "Access token has expired.";
                    return null;
                }
            }
            catch (Exception)
            {
                error = "Failed to validate access token.";
                return null;
            }

            return null;
        }

        private static string GenerateSecureRefreshToken()
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
