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
            services.AddScoped<IResumeDetailService, ResumeDetailService>();

            #endregion

            #region Repositories
            services.AddTransient<IResumeRepository, ResumeRepository>();
            services.AddTransient<IResumeDetailRepository, ResumeDetailRepository>();

            #endregion

            #region Mapper
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Resume, ResumeViewModel>();
                cfg.CreateMap<ResumeViewModel, Resume>();
                cfg.CreateMap<ResumeDetail, ResumeDetailViewModel>();
                cfg.CreateMap<ResumeDetailViewModel, ResumeDetail>();
            });

            IMapper mapper = configuration.CreateMapper();

            // Register the IMapperService implementation with your dependency injection container
            services.AddSingleton<IBaseMapper<Resume, ResumeViewModel>>(new BaseMapper<Resume, ResumeViewModel>(mapper));
            services.AddSingleton<IBaseMapper<ResumeViewModel, Resume>>(new BaseMapper<ResumeViewModel, Resume>(mapper));
            services.AddSingleton<IBaseMapper<ResumeDetail, ResumeDetailViewModel>>(new BaseMapper<ResumeDetail, ResumeDetailViewModel>(mapper));
            services.AddSingleton<IBaseMapper<ResumeDetailViewModel, ResumeDetail>>(new BaseMapper<ResumeDetailViewModel, ResumeDetail>(mapper));

            #endregion

            return services;
        }
    }
}
