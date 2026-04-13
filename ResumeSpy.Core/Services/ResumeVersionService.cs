using ResumeSpy.Core.Entities.Business;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.IRepositories;
using ResumeSpy.Core.Interfaces.IServices;

namespace ResumeSpy.Core.Services
{
    public class ResumeVersionService : IResumeVersionService
    {
        private const int MaxVersionsPerDetail = 50;
        private const int PreviewLength = 100;

        private readonly IResumeVersionRepository _versionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ResumeVersionService(IResumeVersionRepository versionRepository, IUnitOfWork unitOfWork)
        {
            _versionRepository = versionRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<ResumeVersionViewModel>> GetVersionsAsync(string resumeDetailId)
        {
            var versions = await _versionRepository.GetByResumeDetailIdAsync(resumeDetailId);
            return versions.Select(ToViewModel).ToList();
        }

        public async Task<ResumeVersionViewModel> SaveVersionAsync(string resumeDetailId, string content, string? label = null)
        {
            var version = ResumeVersion.Create(resumeDetailId, content, label);
            await _versionRepository.AddAsync(version);

            // Prune oldest versions beyond the cap
            var existing = await _versionRepository.GetByResumeDetailIdAsync(resumeDetailId);
            var toDelete = existing
                .OrderBy(v => v.CreatedAt)
                .Take(Math.Max(0, existing.Count + 1 - MaxVersionsPerDetail))
                .ToList();

            foreach (var old in toDelete)
                await _versionRepository.DeleteAsync(old.Id);

            await _unitOfWork.SaveChangesAsync();
            return ToViewModel(version);
        }

        public async Task DeleteVersionAsync(Guid id)
        {
            await _versionRepository.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<string> GetVersionContentAsync(Guid id)
        {
            var version = await _versionRepository.GetByIdAsync(id);
            if (version == null)
                throw new KeyNotFoundException($"Version {id} not found.");
            return version.Content;
        }

        private static ResumeVersionViewModel ToViewModel(ResumeVersion v) => new()
        {
            Id = v.Id,
            ResumeDetailId = v.ResumeDetailId,
            Content = v.Content,
            Preview = v.Content.Length <= PreviewLength
                ? v.Content
                : v.Content[..PreviewLength] + "…",
            Label = v.Label,
            CreatedAt = v.CreatedAt
        };
    }
}
