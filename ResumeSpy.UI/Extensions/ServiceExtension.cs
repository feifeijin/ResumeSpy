using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using ResumeSpy.Core.Entities.Business;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.AI;
using ResumeSpy.Core.Interfaces.IMapper;
using ResumeSpy.Core.Interfaces.IRepositories;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.Core.Interfaces.Repositories;
using ResumeSpy.Core.Interfaces.Services;
using ResumeSpy.Core.Mapper;
using ResumeSpy.Core.Services;
using ResumeSpy.Infrastructure.Services.AI;
using ResumeSpy.Infrastructure.Repositories;
using ResumeSpy.Infrastructure.Services;
using ResumeSpy.Infrastructure.Services.Translation;
using ResumeSpy.UI.Filters;
using ResumeSpy.UI.Services;

namespace ResumeSpy.UI.Extensions
{
    public static class ServiceExtension
    {
        public static IServiceCollection RegisterService(this IServiceCollection services)
        {
            #region Background Services
            // ThumbnailBackgroundService is a singleton that acts as both the hosted service
            // and the IThumbnailQueue implementation consumed by scoped services.
            services.AddSingleton<ThumbnailBackgroundService>();
            services.AddSingleton<IThumbnailQueue>(sp => sp.GetRequiredService<ThumbnailBackgroundService>());
            services.AddHostedService(sp => sp.GetRequiredService<ThumbnailBackgroundService>());

            // DatabaseWarmupService fires a trivial SELECT 1 at startup so the Npgsql
            // connection pool has at least one open connection before the first user
            // request arrives. This eliminates the 1-3 s cold-start latency that users
            // would otherwise experience the first time they open the resume list.
            services.AddHostedService<DatabaseWarmupService>();
            #endregion

            #region Services
            services.AddScoped<IIdentityLinkingService, IdentityLinkingService>();
            services.AddScoped<IResumeService, ResumeService>();
            services.AddScoped<IResumeDetailService, ResumeDetailService>();
            services.AddScoped<IResumeManagementService, ResumeManagementService>();
            // Supabase Storage uploads / deletes go through this typed client. The
            // standard resilience handler adds bounded retries, a circuit breaker
            // that protects us if Supabase Storage goes down, and explicit per-attempt
            // and total-request timeouts so a hung upload cannot tie up a request
            // for minutes on end.
            services.AddHttpClient<IImageGenerationService, ImageGenerationService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
            })
            .AddStandardResilienceHandler(AddStorageResilience);
            services.AddScoped<IAnonymousUserService, AnonymousUserService>();
            services.AddScoped<IPdfExportService, PdfExportService>();
            services.AddScoped<IResumeTailoringService, ResumeTailoringService>();
            services.AddScoped<IResumeImportService, ResumeImportService>();
            services.AddScoped<IResumeVersionService, ResumeVersionService>();
            services.AddScoped<IResumeChatService, ResumeChatService>();

            // Translation Services. A named HttpClient ("Translation") routes through
            // the same resilience handler as the AI providers — translation endpoints
            // (DeepL / Microsoft / Libre) are themselves third-party calls that
            // periodically blip, and a transient 502 should not fail the user's request.
            services.AddHttpClient(TranslatorFactory.HttpClientName, client =>
            {
                // Generous HttpClient ceiling: the resilience handler's TotalRequestTimeout
                // (90 s, incl. retries) is the real deadline. HttpClient.Timeout must sit
                // above it or it will pre-empt the retry budget.
                client.Timeout = TimeSpan.FromSeconds(120);
            })
            .AddStandardResilienceHandler(AddOutboundResilience);
            services.AddScoped<ITranslationService, TranslationService>();

            // AI quota + access filter for import / chat / tailor endpoints.
            // Singleton because the in-memory counter must be shared across requests.
            services.AddSingleton<IAiQuotaService, InMemoryAiQuotaService>();
            services.AddScoped<AiAccessFilter>();

            #endregion

            #region AI Services
            // Register AI providers with keyed services.
            // HuggingFace uses a named HttpClient so we can configure a request timeout
            // independently of the default client. The standard resilience handler adds
            // bounded retries, a circuit breaker (so a sustained HuggingFace outage stops
            // wasting time on doomed calls and the orchestrator falls through to the next
            // provider faster), and per-attempt + total-request timeouts. The 30-second
            // HttpClient.Timeout is kept as a final safety net.
            services.AddHttpClient("HuggingFace", client =>
            {
                // See TranslatorFactory.HttpClientName registration: HttpClient.Timeout
                // sits above the resilience handler's TotalRequestTimeout so the
                // retry budget is not cut short.
                client.Timeout = TimeSpan.FromSeconds(120);
            })
            .AddStandardResilienceHandler(AddOutboundResilience);
            services.AddKeyedSingleton<IGenerativeTextService, OpenAITextService>("OpenAI");
            services.AddKeyedSingleton<IGenerativeTextService>("HuggingFace", (sp, _) =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = factory.CreateClient("HuggingFace");
                var config = sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ResumeSpy.Infrastructure.Services.AI.HuggingFaceTextService>>();
                return new ResumeSpy.Infrastructure.Services.AI.HuggingFaceTextService(httpClient, config, logger);
            });

            // Register the AI Orchestrator
            services.AddScoped<ResumeSpy.Infrastructure.Services.AI.AIOrchestratorService>();

            #endregion

            #region Repositories
            services.AddScoped<IResumeRepository, ResumeRepository>();
            services.AddScoped<IResumeDetailRepository, ResumeDetailRepository>();
            services.AddScoped<IAnonymousUserRepository, AnonymousUserRepository>();
            services.AddScoped<IResumeVersionRepository, ResumeVersionRepository>();
            services.AddScoped<IUserIdentityRepository, UserIdentityRepository>();
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

        /// <summary>
        /// Resilience profile for outbound AI / translation HTTP calls. Both classes
        /// of upstream are flaky (free-tier rate-limits, model cold-starts, occasional
        /// 502s), so a small retry budget plus a circuit breaker keeps a single bad
        /// minute from cascading into user-facing failures, while the total timeout
        /// caps the worst-case wall-clock latency a caller can experience.
        /// </summary>
        private static void AddOutboundResilience(HttpStandardResilienceOptions options)
        {
            // Cap the entire pipeline (incl. retries) at 90s. The HuggingFace import
            // path is also fenced by a 120s request-level deadline in
            // ResumeImportController, so the per-HTTP-attempt budget stays below it.
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(90);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);

            // 3 retries with jittered exponential backoff. Defaults retry only on
            // transient HttpRequestException / 5xx / 408, which is what we want —
            // do NOT retry on 4xx (auth / quota) since that would just burn quota.
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;
            options.Retry.Delay = TimeSpan.FromMilliseconds(500);

            // Circuit breaker: if >50% of requests fail in a 30s window (with at
            // least 10 samples), break for 30s. Prevents pile-ups when the upstream
            // is genuinely down and lets the AIOrchestrator fall back faster.
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.MinimumThroughput = 10;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Resilience profile for Supabase Storage. Tighter than the AI profile
        /// because storage round-trips should be sub-second and we don't want the
        /// thumbnail-upload background work to monopolise a worker for long.
        /// </summary>
        private static void AddStorageResilience(HttpStandardResilienceOptions options)
        {
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);

            options.Retry.MaxRetryAttempts = 3;
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;
            options.Retry.Delay = TimeSpan.FromMilliseconds(200);

            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.MinimumThroughput = 10;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
        }
    }
}
