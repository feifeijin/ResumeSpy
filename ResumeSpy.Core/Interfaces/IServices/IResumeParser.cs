using ResumeSpy.Core.Entities.Export;

namespace ResumeSpy.Core.Interfaces.IServices
{
    public interface IResumeParser
    {
        ResumeDocument Parse(string markdown, string title);
    }
}
