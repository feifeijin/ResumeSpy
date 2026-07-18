namespace ResumeSpy.Core.Interfaces.IServices
{
    public interface IResumeTailoringService
    {
        Task<string> TailorResumeAsync(string resumeContent, string jobDescription, string? language = null);
    }
}
