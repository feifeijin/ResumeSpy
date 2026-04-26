using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.IRepositories;
using ResumeSpy.Core.Interfaces.IServices;

namespace ResumeSpy.Infrastructure.Services
{
    public class PromptProviderService : IPromptProviderService
    {
        private readonly IPromptRepository _repo;
        private readonly IUnitOfWork _unitOfWork;

        public PromptProviderService(IPromptRepository repo, IUnitOfWork unitOfWork)
        {
            _repo = repo;
            _unitOfWork = unitOfWork;
        }

        public async Task<string> GetSystemMessageAsync(string key, string fallback)
        {
            var template = await _repo.GetByKeyAsync(key);
            return template?.SystemMessage ?? fallback;
        }

        public async Task<IEnumerable<PromptTemplate>> GetAllAsync()
        {
            return await _repo.GetAll();
        }

        public async Task UpsertAsync(PromptTemplate template)
        {
            var existing = await _repo.GetByKeyAsync(template.Key);
            if (existing == null)
            {
                template.EntryDate = DateTime.UtcNow;
                template.UpdateDate = DateTime.UtcNow;
                await _repo.Create(template);
            }
            else
            {
                existing.SystemMessage = template.SystemMessage;
                existing.Description = template.Description;
                existing.IsActive = template.IsActive;
                existing.Version++;
                existing.UpdateDate = DateTime.UtcNow;
                await _repo.Update(existing);
            }
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAsync(string key)
        {
            var existing = await _repo.GetByKeyAsync(key);
            if (existing != null)
            {
                await _repo.Delete(existing);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}
