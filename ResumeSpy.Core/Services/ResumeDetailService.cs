using ResumeSpy.Core.Entities.Business;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Exceptions;
using ResumeSpy.Core.Interfaces.IMapper;
using ResumeSpy.Core.Interfaces.IRepositories;
using ResumeSpy.Core.Interfaces.IServices;

namespace ResumeSpy.Core.Services
{
    public class ResumeDetailService : IResumeDetailService
    {
        private readonly IBaseMapper<ResumeDetail, ResumeDetailViewModel> _resumeDetailViewModelMapper;
        private readonly IBaseMapper<ResumeDetailViewModel, ResumeDetail> _resumeDetailMapper;
        private readonly IResumeDetailRepository _resumeDetailRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IImageGenerationService _imageGenerationService;
        
        public ResumeDetailService(
            IBaseMapper<ResumeDetail, ResumeDetailViewModel> resumeDetailViewModelMapper,
            IBaseMapper<ResumeDetailViewModel, ResumeDetail> resumeDetailMapper,
            IResumeDetailRepository resumeDetailRepository,
            IUnitOfWork unitOfWork,
            IImageGenerationService imageGenerationService)
        {
            _resumeDetailViewModelMapper = resumeDetailViewModelMapper;
            _resumeDetailMapper = resumeDetailMapper;
            _resumeDetailRepository = resumeDetailRepository;
            _unitOfWork = unitOfWork;
            _imageGenerationService = imageGenerationService;
        }
        public async Task<ResumeDetailViewModel> Create(ResumeDetailViewModel model)
        {
             var entity = _resumeDetailMapper.MapModel(model);
            entity.EntryDate    = DateTime.UtcNow;     
            var result = await _resumeDetailRepository.Create(entity);
            await _unitOfWork.SaveChangesAsync();
            return _resumeDetailViewModelMapper.MapModel(result);
        }

        public async Task Delete(string id)
        {
            var entity = await _resumeDetailRepository.GetById(id);
            if (entity == null)
            {
                throw new NotFoundException($"ResumeDetail with id {id} not found.");
            }
            await _imageGenerationService.DeleteThumbnailAsync(entity.ResumeImgPath);
            await _resumeDetailRepository.Delete(entity);
            await _unitOfWork.SaveChangesAsync();
        } 

        public async Task<ResumeDetailViewModel> GetResumeDetail(string id)
        {
            var entity = await _resumeDetailRepository.GetById(id);
            if (entity == null)
            {
                throw new NotFoundException($"ResumeDetail with id {id} not found.");
            }
            return _resumeDetailViewModelMapper.MapModel(entity);
        }

        public async Task<IEnumerable<ResumeDetailViewModel>> GetResumeDetailsByResumeId(string resumeId)
        {
            var entities = await _resumeDetailRepository.GetResumeDetailsByResumeIdAsync(resumeId);    
            if (entities == null)
            {
                throw new NotFoundException($"ResumeDetail with id {resumeId} not found.");
            }
            return _resumeDetailViewModelMapper.MapList(entities);
        }


        public Task<bool> IsExists(string key, string value)
        {
            return _resumeDetailRepository.IsExists(key, value);
        }

        public Task<bool> IsExistsForUpdate(string id, string key, string value)
        {
            return _resumeDetailRepository.IsExistsForUpdate(id, key, value);
        }

        public async Task Update(ResumeDetailViewModel model)
        {
            var existingData = await _resumeDetailRepository.GetById(model.Id);
            if (existingData == null)
            {
                throw new NotFoundException($"ResumeDetail with id {model.Id} not found.");
            }

            // Regenerate thumbnail if content has changed
            if (existingData.Content != model.Content && !string.IsNullOrWhiteSpace(model.Content))
            {
                await _imageGenerationService.DeleteThumbnailAsync(existingData.ResumeImgPath);
                existingData.ResumeImgPath = await _imageGenerationService.GenerateThumbnailAsync(model.Content, $"{model.ResumeId}_{model.Id}");
            }
            else if (string.IsNullOrWhiteSpace(model.Content))
            {
                existingData.ResumeImgPath = null; // Or a default placeholder path
            }

            existingData.Content = model.Content;
            existingData.Name = model.Name;
            existingData.UpdateDate = DateTime.UtcNow;
            existingData.IsDefault = model.IsDefault;
            existingData.Language = model.Language;
            await _resumeDetailRepository.Update(existingData);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}