using ResumeSpy.Core.Entities.General;

namespace ResumeSpy.Core.Interfaces.Repositories
{
    public interface IUserIdentityRepository
    {
        /// <summary>Returns the identity (with User included) if it exists, otherwise null.</summary>
        Task<UserIdentity?> FindByProviderAsync(string provider, string providerUserId);

        Task<UserIdentity> CreateAsync(UserIdentity identity);

        /// <summary>Stamps LastLoginAt = UtcNow without loading the full entity.</summary>
        Task UpdateLastLoginAsync(int id);
    }
}
