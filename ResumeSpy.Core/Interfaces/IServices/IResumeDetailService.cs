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
        Task<IEnumerable<ResumeDetailViewModel>> GetResumeDetails();
        Task<PaginatedDataViewModel<ResumeDetailViewModel>> GetPaginatedResumeDetails(int pageNumber, int pageSize);
        Task<ResumeDetailViewModel> GetResumeDetail(int id);
        Task<bool> IsExists(string key, string value);
        Task<bool> IsExistsForUpdate(int id, string key, string value);
        Task<ResumeDetailViewModel> Create(ResumeDetailViewModel model);
        Task Update(ResumeDetailViewModel model);
        Task Delete(int id);
    }
}
