using System.Threading;
using System.Threading.Tasks;
using ResumeSpy.Core.Entities.Business.Auth;

namespace ResumeSpy.Core.Interfaces.IServices
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
        Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
        Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
        Task LogoutAsync(string userId, LogoutRequest request, CancellationToken cancellationToken = default);
        Task<AuthResponse> ExternalLoginAsync(ExternalAuthRequest request, CancellationToken cancellationToken = default);
        Task<AuthResponse> RequestEmailLinkAsync(EmailLinkRequest request, CancellationToken cancellationToken = default);
        Task<AuthResponse> ConfirmEmailLinkAsync(ConfirmEmailLinkRequest request, CancellationToken cancellationToken = default);
    }
}
