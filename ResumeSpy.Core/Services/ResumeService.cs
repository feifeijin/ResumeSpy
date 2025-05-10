using ResumeSpy.Core.Entities.Business;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Exceptions;
using ResumeSpy.Core.Interfaces.IMapper;
using ResumeSpy.Core.Interfaces.IRepositories;
using ResumeSpy.Core.Interfaces.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumeSpy.Core.Services
{
    public class ResumeService : IResumeService
    {
        private readonly IBaseMapper<Resume, ResumeViewModel> _resumeViewModelMapper;
        private readonly IBaseMapper<ResumeViewModel, Resume> _resumeMapper;
        private readonly IResumeRepository _resumeRepository;

        public ResumeService(
            IBaseMapper<Resume, ResumeViewModel> resumeViewModelMapper,
            IBaseMapper<ResumeViewModel, Resume> resumeMapper,
            IResumeRepository resumeRepository)
        {
            _resumeViewModelMapper = resumeViewModelMapper;
            _resumeMapper = resumeMapper;
            _resumeRepository = resumeRepository;
        }

        public async Task<ResumeViewModel> Create(ResumeViewModel model)
        {
            var entity = _resumeMapper.MapModel(model);
            entity.EntryDate    = DateTime.Now;     
            return _resumeViewModelMapper.MapModel(await _resumeRepository.Create(entity));
        }

        public async Task Delete(string id)
        {
            var entity = await _resumeRepository.GetById(id);
            if (entity == null)
            {
                throw new NotFoundException($"Resume with id {id} not found.");
            }
            await _resumeRepository.Delete(entity);
        }

        public  async Task<PaginatedDataViewModel<ResumeViewModel>> GetPaginatedResumes(int pageNumber, int pageSize)
        {
            var paginatedData = await _resumeRepository.GetPaginatedData(pageNumber, pageSize);

            var mappedData= _resumeViewModelMapper.MapList(paginatedData.Data);

            var PaginatedDataViewModel = new PaginatedDataViewModel<ResumeViewModel>(mappedData.ToList(), paginatedData.TotalCount);
            return PaginatedDataViewModel;
        }

        public async Task<ResumeViewModel> GetResume(string id)
        {
            var entity = await _resumeRepository.GetById(id);
            if (entity == null)
            {
                throw new NotFoundException($"Resume with id {id} not found.");
            }
           return _resumeViewModelMapper.MapModel(entity);
        }

        public async Task<IEnumerable<ResumeViewModel>> GetResumes()
        {
            return _resumeViewModelMapper.MapList(await _resumeRepository.GetAll());

        }

        public async Task<bool> IsExists(string key, string value)
        {
            return await _resumeRepository.IsExists(key, value);
        }

        public async Task<bool> IsExistsForUpdate(string id, string key, string value)
        {
            return await _resumeRepository.IsExistsForUpdate(id, key, value);
        }

        public async Task Update(ResumeViewModel model)
        {
            var existingData = await _resumeRepository.GetById(model.Id);
            existingData.ResumeDetailCount = model.ResumeDetailCount;
            existingData.ResumeImgPath = model.ResumeImgPath;
            existingData.Title = model.Title;
            existingData.UpdateDate = DateTime.Now;

            await _resumeRepository.Update(existingData);
        }
    }
}