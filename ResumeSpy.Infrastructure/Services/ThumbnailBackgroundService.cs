using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ResumeSpy.Core.Interfaces.IRepositories;
using ResumeSpy.Core.Interfaces.IServices;

namespace ResumeSpy.Infrastructure.Services
{
    /// <summary>
    /// Background service that processes thumbnail generation tasks off the hot save path.
    /// Implements <see cref="IThumbnailQueue"/> so that scoped services (ResumeDetailService)
    /// can enqueue work without injecting the hosted service directly.
    /// </summary>
    public sealed class ThumbnailBackgroundService : BackgroundService, IThumbnailQueue
    {
        // Bounded channel: at most 200 pending tasks.
        // DropOldest ensures we never block the caller — stale tasks are discarded.
        private readonly Channel<ThumbnailTask> _channel = Channel.CreateBounded<ThumbnailTask>(
            new BoundedChannelOptions(200)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false,
            });

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ThumbnailBackgroundService> _logger;

        public ThumbnailBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<ThumbnailBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <inheritdoc/>
        public void Enqueue(ThumbnailTask task)
        {
            // TryWrite never blocks; returns false only when the channel is full,
            // in which case DropOldest already freed a slot before this call completes.
            _channel.Writer.TryWrite(task);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Thumbnail background service started.");

            await foreach (var task in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessTaskAsync(task, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Thumbnail background processing failed for detail {Id}.", task.ResumeDetailId);
                }
            }

            _logger.LogInformation("Thumbnail background service stopped.");
        }

        private async Task ProcessTaskAsync(ThumbnailTask task, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var imageService = scope.ServiceProvider.GetRequiredService<IImageGenerationService>();
            var detailRepo   = scope.ServiceProvider.GetRequiredService<IResumeDetailRepository>();
            var resumeRepo   = scope.ServiceProvider.GetRequiredService<IResumeRepository>();
            var unitOfWork   = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Delete the old thumbnail first (ignore errors — it may already be gone)
            if (!string.IsNullOrWhiteSpace(task.OldImagePath))
                await imageService.DeleteThumbnailAsync(task.OldImagePath);

            // If content is empty, clear the thumbnail and bail out
            if (string.IsNullOrWhiteSpace(task.Content))
            {
                var emptyEntity = await detailRepo.GetById(task.ResumeDetailId);
                if (emptyEntity is not null)
                {
                    emptyEntity.ResumeImgPath = null;
                    await detailRepo.Update(emptyEntity);

                    if (emptyEntity.IsDefault)
                        await SyncResumeImgPathAsync(resumeRepo, task.ResumeId, null);
                }
                await unitOfWork.SaveChangesAsync();
                return;
            }

            // Generate and upload the new thumbnail
            var newPath = await imageService.GenerateThumbnailAsync(
                task.Content,
                $"{task.ResumeId}_{task.ResumeDetailId}");

            // Persist the new path on the detail
            var entity = await detailRepo.GetById(task.ResumeDetailId);
            if (entity is null)
            {
                _logger.LogWarning("ResumeDetail {Id} not found when persisting new thumbnail.", task.ResumeDetailId);
                return;
            }

            entity.ResumeImgPath = newPath;
            await detailRepo.Update(entity);

            // Keep Resume.ResumeImgPath in sync so the dossier card always shows
            // the default detail's latest thumbnail.
            if (entity.IsDefault)
                await SyncResumeImgPathAsync(resumeRepo, task.ResumeId, newPath);

            await unitOfWork.SaveChangesAsync();

            _logger.LogDebug("Thumbnail updated for detail {Id} (isDefault={IsDefault}).", task.ResumeDetailId, entity.IsDefault);
        }

        private async Task SyncResumeImgPathAsync(IResumeRepository resumeRepo, string resumeId, string? imgPath)
        {
            var resume = await resumeRepo.GetById(resumeId);
            if (resume is null) return;
            resume.ResumeImgPath = imgPath;
            await resumeRepo.Update(resume);
        }
    }
}
