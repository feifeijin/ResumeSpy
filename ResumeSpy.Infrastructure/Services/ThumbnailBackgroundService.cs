using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ResumeSpy.Core.Interfaces.IRepositories;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.Core.Interfaces.Repositories;

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
            var repo        = scope.ServiceProvider.GetRequiredService<IResumeDetailRepository>();
            var unitOfWork  = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Delete the old thumbnail first (ignore errors — it may already be gone)
            if (!string.IsNullOrWhiteSpace(task.OldImagePath))
                await imageService.DeleteThumbnailAsync(task.OldImagePath);

            // If content is empty, there is nothing to generate — just clean up
            if (string.IsNullOrWhiteSpace(task.Content))
            {
                var emptyEntity = await repo.GetById(task.ResumeDetailId);
                if (emptyEntity is not null)
                {
                    emptyEntity.ResumeImgPath = null;
                    await repo.Update(emptyEntity);
                    await unitOfWork.SaveChangesAsync();
                }
                return;
            }

            // Generate and upload the new thumbnail
            var newPath = await imageService.GenerateThumbnailAsync(
                task.Content,
                $"{task.ResumeId}_{task.ResumeDetailId}");

            // Persist the new image path
            var entity = await repo.GetById(task.ResumeDetailId);
            if (entity is null)
            {
                _logger.LogWarning("ResumeDetail {Id} not found when persisting new thumbnail.", task.ResumeDetailId);
                return;
            }

            entity.ResumeImgPath = newPath;
            await repo.Update(entity);
            await unitOfWork.SaveChangesAsync();

            _logger.LogDebug("Thumbnail updated for detail {Id}.", task.ResumeDetailId);
        }
    }
}
