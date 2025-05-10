using ResumeSpy.Core.Entities.Business;
using ResumeSpy.Core.Entities.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumeSpy.Core.Interfaces.IServices
{
    public interface IResumeService
    {
        Task<IEnumerable<ResumeViewModel>> GetResumes();
        Task<PaginatedDataViewModel<ResumeViewModel>> GetPaginatedResumes(int pageNumber, int pageSize);
        Task<ResumeViewModel> GetResume(string id);
        Task<bool> IsExists(string key, string value);
        Task<bool> IsExistsForUpdate(string id, string key, string value);
        Task<ResumeViewModel> Create(ResumeViewModel model);
        Task Update(ResumeViewModel model);
        Task Delete(string id);
    }
}
