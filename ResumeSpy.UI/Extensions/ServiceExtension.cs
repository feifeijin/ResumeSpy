using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using ResumeSpy.Core.Entities.Business;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.IMapper;
using ResumeSpy.Core.Interfaces.IRepositories;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.Core.Mapper;
using ResumeSpy.Core.Services;
using ResumeSpy.Infrastructure.Repositories;

namespace ResumeSpy.UI.Extensions
{
    public static class ServiceExtension
    {
        public static IServiceCollection RegisterService(this IServiceCollection services)
        {
            #region Services
            services.AddScoped<IResumeService, ResumeService>();

            #endregion

            #region Repositories
            services.AddTransient<IResumeRepository, ResumeRepository>();

            #endregion

            #region Mapper
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Resume, ResumeViewModel>();
                cfg.CreateMap<ResumeViewModel, Resume>();
            });

            IMapper mapper = configuration.CreateMapper();

            // Register the IMapperService implementation with your dependency injection container
            services.AddSingleton<IBaseMapper<Resume, ResumeViewModel>>(new BaseMapper<Resume, ResumeViewModel>(mapper));
            services.AddSingleton<IBaseMapper<ResumeViewModel, Resume>>(new BaseMapper<ResumeViewModel, Resume>(mapper));

            #endregion

            return services;
        }
    }
}
