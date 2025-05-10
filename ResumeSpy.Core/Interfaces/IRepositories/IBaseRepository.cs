using ResumeSpy.Core.Entities.Business;
using ResumeSpy.Core.Entities.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ResumeSpy.Core.Interfaces.IRepositories
{
    //Unit of Work Pattern
    public interface IBaseRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAll();
        Task<PaginatedDataViewModel<T>> GetPaginatedData(int pageNumber, int pageSize);
        Task<T> GetById<Tid>(Tid id);
        Task<bool> IsExists<Tvalue>(string key, Tvalue value);
        Task<bool> IsExistsForUpdate<Tid>(Tid id, string key, string value);
        Task<T> Create(T model);
        Task Update(T model);
        Task Delete(T model);
        Task SaveChangeAsync();
    }
}
