namespace ResumeSpy.Core.Interfaces.IServices
{
    public interface IPdfExportService
    {
        Task<byte[]> GeneratePdfAsync(string content, string title);
    }
}
