using ResumeSpy.Core.Entities.General;

namespace ResumeSpy.Core.Interfaces.IRepositories
{
    public interface IPromptRepository : IBaseRepository<PromptTemplate>
    {
        Task<PromptTemplate?> GetByKeyAsync(string key);
    }
}
