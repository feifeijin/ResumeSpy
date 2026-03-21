using Microsoft.EntityFrameworkCore;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.IRepositories;
using ResumeSpy.Infrastructure.Data;
using System;
using System.Threading.Tasks;

namespace ResumeSpy.Infrastructure.Repositories
{
    public class AnonymousUserRepository : BaseRepository<AnonymousUser>, IAnonymousUserRepository
    {
        public AnonymousUserRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<AnonymousUser?> FindByIdAsync(Guid anonymousUserId)
        {
            return await _dbContext.AnonymousUsers.FindAsync(anonymousUserId);
        }

        public async Task<AnonymousUser?> GetByIdForUpdateAsync(Guid anonymousUserId)
        {
            return await _dbContext.AnonymousUsers
                .FromSqlInterpolated($"SELECT * FROM \"AnonymousUsers\" WHERE \"Id\" = {anonymousUserId} FOR UPDATE")
                .FirstOrDefaultAsync();
        }
    }
}
