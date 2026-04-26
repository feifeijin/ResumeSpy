using Microsoft.EntityFrameworkCore;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.IRepositories;
using ResumeSpy.Infrastructure.Data;

namespace ResumeSpy.Infrastructure.Repositories
{
    public class PromptRepository : BaseRepository<PromptTemplate>, IPromptRepository
    {
        public PromptRepository(ApplicationDbContext dbContext) : base(dbContext) { }

        public async Task<PromptTemplate?> GetByKeyAsync(string key)
        {
            return await DbSet
                .FirstOrDefaultAsync(p => p.Key == key && p.IsActive);
        }
    }
}
