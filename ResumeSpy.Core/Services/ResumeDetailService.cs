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
        
        public ResumeDetailService(
            IBaseMapper<ResumeDetail, ResumeDetailViewModel> resumeDetailViewModelMapper,
            IBaseMapper<ResumeDetailViewModel, ResumeDetail> resumeDetailMapper,
            IResumeDetailRepository resumeDetailRepository)
        {
            _resumeDetailViewModelMapper = resumeDetailViewModelMapper;
            _resumeDetailMapper = resumeDetailMapper;
            _resumeDetailRepository = resumeDetailRepository;
        }
        public async Task<ResumeDetailViewModel> Create(ResumeDetailViewModel model)
        {
             var entity = _resumeDetailMapper.MapModel(model);
            entity.EntryDate    = DateTime.Now;     
            return _resumeDetailViewModelMapper.MapModel(await _resumeDetailRepository.Create(entity));
       
            throw new NotImplementedException();
        }

        public async Task Delete(string id)
        {
            var entity = await _resumeDetailRepository.GetById(id);
            if (entity == null)
            {
                throw new NotFoundException($"ResumeDetail with id {id} not found.");
            }
            await _resumeDetailRepository.Delete(entity);
        } 

        public async Task<PaginatedDataViewModel<ResumeDetailViewModel>> GetPaginatedResumeDetails(int pageNumber, int pageSize)
        {
            var paginatedData = await _resumeDetailRepository.GetPaginatedData(pageNumber, pageSize);
            var mappedData= _resumeDetailViewModelMapper.MapList(paginatedData.Data);
            var PaginatedDataViewModel = new PaginatedDataViewModel<ResumeDetailViewModel>(mappedData.ToList(), paginatedData.TotalCount);
            return PaginatedDataViewModel;
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
            var entities = await _resumeDetailRepository.GetResumeDetailsByResumeId(resumeId);    
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
            existingData.Content = model.Content;
            existingData.Name = model.Name;
            existingData.UpdateDate = DateTime.Now;
            existingData.IsDefault = model.IsDefault;
            existingData.Language = model.Language;
            await _resumeDetailRepository.Update(existingData);
        }
    }
}