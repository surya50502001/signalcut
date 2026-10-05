using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SignalCut.Application.Common;
using SignalCut.Application.DTOs;
using SignalCut.Application.Interfaces;
using SignalCut.Domain.Entities;
using SignalCut.Domain.Enums;

namespace SignalCut.Infrastructure.Services;

public class RenderJobBackgroundService : BackgroundService
{
    private readonly IRenderJobQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RenderJobBackgroundService> _logger;

    public RenderJobBackgroundService(
        IRenderJobQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<RenderJobBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RenderJobBackgroundService started. Reconciling pending QUEUED jobs from database...");

        // 1. Recover any pending QUEUED jobs from database (durable restart recovery)
        try
        {
            using var initScope = _scopeFactory.CreateScope();
            var context = initScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var pendingJobs = await context.Jobs
                .Where(j => j.Type == JobType.VIDEO_RENDER && j.Status == JobStatus.QUEUED)
                .OrderBy(j => j.CreatedAt)
                .ToListAsync(stoppingToken);

            if (pendingJobs.Count > 0)
            {
                _logger.LogInformation("Enqueuing {Count} recovered QUEUED render jobs from database.", pendingJobs.Count);
                foreach (var pj in pendingJobs)
                {
                    Guid clipId = Guid.Empty;
                    try
                    {
                        if (!string.IsNullOrEmpty(pj.PayloadJson))
                        {
                            using var doc = JsonDocument.Parse(pj.PayloadJson);
                            if (doc.RootElement.TryGetProperty("ClipId", out var prop))
                            {
                                clipId = prop.GetGuid();
                            }
                        }
                    }
                    catch (Exception jsonEx)
                    {
                        _logger.LogWarning(jsonEx, "Failed to parse PayloadJson for Job {JobId}", pj.Id);
                    }

                    if (clipId != Guid.Empty)
                    {
                        await _queue.EnqueueAsync(new RenderJobQueueItem(pj.OrganizationId, pj.Id, clipId, pj.ReservedCredits), stoppingToken);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recover pending QUEUED render jobs on startup.");
        }

        // 2. Continuously process queued rendering jobs
        await foreach (var item in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await ProcessRenderItemAsync(item, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception processing render queue item for Job {JobId}", item.JobId);
            }
        }
    }

    private async Task ProcessRenderItemAsync(RenderJobQueueItem item, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var walletService = scope.ServiceProvider.GetRequiredService<ICreditWalletService>();
        var videoProcessor = scope.ServiceProvider.GetRequiredService<IVideoProcessor>();

        var job = await context.Jobs.FirstOrDefaultAsync(j => j.Id == item.JobId, ct);
        var clip = await context.Clips
            .Include(c => c.Moment)
                .ThenInclude(m => m.Source)
            .FirstOrDefaultAsync(c => c.Id == item.ClipId, ct);

        if (job == null || clip == null)
        {
            _logger.LogWarning("Render job {JobId} or Clip {ClipId} not found in database.", item.JobId, item.ClipId);
            return;
        }

        // Avoid re-processing if already completed or canceled
        if (job.Status == JobStatus.COMPLETED || job.Status == JobStatus.CANCELLED)
        {
            return;
        }

        job.Status = JobStatus.PROCESSING;
        job.StartedAt = DateTime.UtcNow;
        job.ProgressPercentage = 20;
        job.CurrentStep = "Extracting video frames and applying 9:16 smart crop";
        await context.SaveChangesAsync(ct);

        try
        {
            // Resolve media asset
            var mediaAsset = await context.MediaAssets
                .Where(m => m.SourceId == clip.Moment.SourceId)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (mediaAsset == null || string.IsNullOrWhiteSpace(mediaAsset.StorageUrl))
            {
                throw new UnauthorizedMediaException(clip.Moment.SourceId,
                    "Cannot render clip: No authorized media file found for this source. Discovered sources require uploaded media.");
            }

            var sourceVideoUrl = mediaAsset.StorageUrl;
            if (!string.IsNullOrEmpty(sourceVideoUrl) && sourceVideoUrl.StartsWith("/storage/"))
            {
                var relative = sourceVideoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var localPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relative);
                if (File.Exists(localPath)) sourceVideoUrl = localPath;
            }

            // Strictly ensure external streaming platforms are blocked
            if (sourceVideoUrl.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) ||
                sourceVideoUrl.Contains("youtu.be", StringComparison.OrdinalIgnoreCase) ||
                sourceVideoUrl.Contains("vimeo.com", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedMediaException(clip.Moment.SourceId,
                    "Direct rendering from external video platforms is not permitted. An authorized media file must be uploaded.");
            }

            var captionsList = new List<TranscriptChunkResult>
            {
                new(0, clip.StartTime, clip.EndTime, clip.Hook, clip.Moment.Speaker, 0.98)
            };

            var renderReq = new RenderVideoRequest(
                clip.Id,
                sourceVideoUrl,
                clip.StartTime,
                clip.EndTime,
                clip.AspectRatio,
                clip.ResolutionWidth,
                clip.ResolutionHeight,
                clip.HasWatermark,
                clip.CaptionStyle,
                clip.FontName,
                clip.FontSize,
                clip.PrimaryColorHex,
                clip.HighlightColorHex,
                clip.BackgroundColorHex,
                null,
                clip.ShowProgressBar,
                captionsList
            );

            var renderResult = await videoProcessor.RenderClipAsync(renderReq, progress =>
            {
                job.ProgressPercentage = progress;
            }, ct);

            if (renderResult.Success)
            {
                clip.RenderedVideoStorageKey = renderResult.StorageKey;
                clip.RenderedVideoUrl = renderResult.StorageUrl;
                clip.ThumbnailUrl = renderResult.ThumbnailUrl;
                clip.RenderStatus = JobStatus.COMPLETED;

                job.Status = JobStatus.COMPLETED;
                job.ProgressPercentage = 100;
                job.CurrentStep = "Render completed successfully";
                job.CompletedAt = DateTime.UtcNow;
                job.ActualCreditsConsumed = item.RequiredCredits;
                job.IsCreditFinalized = true;

                await context.SaveChangesAsync(ct);
                await walletService.CommitReservedCreditsAsync(item.OrganizationId, item.JobId, item.RequiredCredits, $"Video Render for Clip {clip.Id}", ct);
                _logger.LogInformation("Render completed successfully for Job {JobId} and Clip {ClipId}", item.JobId, clip.Id);
            }
            else
            {
                throw new InvalidOperationException(renderResult.ErrorMessage ?? "Video processor reported failure.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rendering failed for Job {JobId} on Clip {ClipId}", item.JobId, clip.Id);

            job.Status = JobStatus.FAILED;
            job.ErrorMessage = ex.Message;
            job.CurrentStep = "Render failed";
            job.CompletedAt = DateTime.UtcNow;
            clip.RenderStatus = JobStatus.FAILED;

            await context.SaveChangesAsync(ct);

            try
            {
                await walletService.RefundReservedCreditsAsync(item.OrganizationId, item.JobId, $"Refund: render failure - {ex.Message}", ct);
            }
            catch (Exception refundEx)
            {
                _logger.LogError(refundEx, "Failed to refund credits for failed Job {JobId}", item.JobId);
            }
        }
    }
}
