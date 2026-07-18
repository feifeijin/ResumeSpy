using ResumeSpy.Core.Entities.Export;

namespace ResumeSpy.Core.Interfaces.IServices
{
    public interface IResumeExporter<TOutput>
    {
        Task<TOutput> ExportAsync(ResumeDocument resume);
    }
}
