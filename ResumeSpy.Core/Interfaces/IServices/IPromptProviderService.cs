using ResumeSpy.Core.Entities.General;

namespace ResumeSpy.Core.Interfaces.IServices
{
    public interface IPromptProviderService
    {
        Task<string> GetSystemMessageAsync(string key, string fallback);
        Task<IEnumerable<PromptTemplate>> GetAllAsync();
        Task UpsertAsync(PromptTemplate template);
        Task DeleteAsync(string key);
    }
}
