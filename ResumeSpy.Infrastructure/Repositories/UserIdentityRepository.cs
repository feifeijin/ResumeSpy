using Microsoft.EntityFrameworkCore;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.Repositories;
using ResumeSpy.Infrastructure.Data;

namespace ResumeSpy.Infrastructure.Repositories
{
    public class UserIdentityRepository : IUserIdentityRepository
    {
        private readonly ApplicationDbContext _context;

        public UserIdentityRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserIdentity?> FindByProviderAsync(string provider, string providerUserId)
        {
            return await _context.UserIdentities
                .Include(i => i.User)
                .FirstOrDefaultAsync(i => i.Provider == provider && i.ProviderUserId == providerUserId);
        }

        public async Task<UserIdentity> CreateAsync(UserIdentity identity)
        {
            _context.UserIdentities.Add(identity);
            return identity;
        }

        public async Task UpdateLastLoginAsync(int id)
        {
            await _context.UserIdentities
                .Where(i => i.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.LastLoginAt, DateTime.UtcNow));
        }
    }
}
