using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using ResumeSpy.Core.Entities.Business;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.AI;
using ResumeSpy.Core.Interfaces.IMapper;
using ResumeSpy.Core.Interfaces.IRepositories;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.Core.Mapper;
using ResumeSpy.Core.Services;
using ResumeSpy.Infrastructure.Services.AI;
using ResumeSpy.Infrastructure.Repositories;
using ResumeSpy.Infrastructure.Services;
using ResumeSpy.Infrastructure.Services.Translation;
using ResumeSpy.Infrastructure.Services.Email;

namespace ResumeSpy.UI.Extensions
{
    public static class ServiceExtension
    {
        public static IServiceCollection RegisterService(this IServiceCollection services)
        {
            #region Services
            services.AddScoped<IResumeService, ResumeService>();
            services.AddScoped<IResumeDetailService, ResumeDetailService>();
            services.AddScoped<IResumeManagementService, ResumeManagementService>();
            services.AddScoped<IImageGenerationService, ImageGenerationService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IEmailSender, SmtpEmailSender>();
            
            // Translation Services
            services.AddScoped<ITranslationService, TranslationService>();

            #endregion

            #region AI Services
            // Register AI providers with keyed services
            services.AddKeyedSingleton<IGenerativeTextService, OpenAITextService>("OpenAI");
            services.AddKeyedSingleton<IGenerativeTextService, HuggingFaceTextService>("HuggingFace");
            
            // Register the AI Orchestrator
            services.AddScoped<ResumeSpy.Infrastructure.Services.AI.AIOrchestratorService>();
            
            #endregion

            #region Repositories
            services.AddScoped<IResumeRepository, ResumeRepository>();
            services.AddScoped<IResumeDetailRepository, ResumeDetailRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

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
