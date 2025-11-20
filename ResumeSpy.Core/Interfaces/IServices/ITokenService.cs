using System.Threading;
using System.Threading.Tasks;
using ResumeSpy.Core.Entities.Business.Auth;
using ResumeSpy.Core.Entities.General;

namespace ResumeSpy.Core.Interfaces.IServices
{
    public interface ITokenService
    {
    Task<TokenResult> GenerateTokenPairAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task<TokenRefreshResult> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
        Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    }
}
