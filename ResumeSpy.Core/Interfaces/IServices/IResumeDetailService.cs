using ResumeSpy.Core.Entities.Business;
using ResumeSpy.Core.Entities.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumeDetailSpy.Core.Interfaces.IServices
{
    public interface IResumeDetailService
    {
        Task<IEnumerable<ResumeDetailViewModel>> GetResumeDetailsByResumeId(string resumeId);
        Task<PaginatedDataViewModel<ResumeDetailViewModel>> GetPaginatedResumeDetails(int pageNumber, int pageSize);
        Task<ResumeDetailViewModel> GetResumeDetail(string id);
        Task<bool> IsExists(string key, string value);
        Task<bool> IsExistsForUpdate(string id, string key, string value);
        Task<ResumeDetailViewModel> Create(ResumeDetailViewModel model);
        Task Update(ResumeDetailViewModel model);
        Task Delete(string id);
    }
}
