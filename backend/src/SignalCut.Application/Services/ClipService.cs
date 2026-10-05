using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SignalCut.Application.Common;
using SignalCut.Application.DTOs;
using SignalCut.Application.Interfaces;
using SignalCut.Domain.Entities;
using SignalCut.Domain.Enums;

namespace SignalCut.Application.Services;

public class ClipService : IClipService
{
    private readonly IApplicationDbContext _context;
    private readonly IRightsAuthorizationService _rightsAuthService;
    private readonly ICreditWalletService _creditWalletService;
    private readonly IVideoProcessor _videoProcessor;
    private readonly ILogger<ClipService> _logger;
    private readonly IServiceScopeFactory? _scopeFactory;

    public ClipService(
        IApplicationDbContext context,
        IRightsAuthorizationService rightsAuthService,
        ICreditWalletService creditWalletService,
        IVideoProcessor videoProcessor,
        ILogger<ClipService> logger,
        IServiceScopeFactory? scopeFactory = null)
    {
        _context = context;
        _rightsAuthService = rightsAuthService;
        _creditWalletService = creditWalletService;
        _videoProcessor = videoProcessor;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }


    public async Task<ClipDto> CreateClipFromMomentAsync(Guid organizationId, Guid userId, CreateClipRequest request, CancellationToken ct = default)
    {
        var moment = await _context.Moments
            .Include(m => m.Source)
            .FirstOrDefaultAsync(m => m.Id == request.MomentId && m.OrganizationId == organizationId, ct);

        if (moment == null)
        {
            throw new NotFoundException(nameof(Moment), request.MomentId);
        }

        // 1. CRITICAL: Assert rights before entering clip creation pipeline
        await _rightsAuthService.AssertCanEnterGenerationPipelineAsync(moment.SourceId, ct);

        var startTime = request.StartTime ?? moment.StartTime;
        var endTime = request.EndTime ?? moment.EndTime;
        if (endTime <= startTime)
        {
            endTime = startTime + 30; // default 30s minimum if bounds invalid
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        bool shouldWatermark = user != null && !user.HasUsedFreeTrial;

        var clip = new Clip
        {
            OrganizationId = organizationId,
            ProjectId = request.ProjectId ?? moment.Source.ProjectId,
            MomentId = moment.Id,
            CreatedByUserId = userId,
            StartTime = startTime,
            EndTime = endTime,
            AspectRatio = request.AspectRatio ?? "9:16",
            ResolutionWidth = 1080,
            ResolutionHeight = 1920,
            HasWatermark = shouldWatermark,
            CaptionStyle = request.CaptionStyle ?? "TIKTOK_POP",
            FontName = "Inter",
            FontSize = 42,
            PrimaryColorHex = "#FFFFFF",
            HighlightColorHex = "#FACC15",
            BackgroundColorHex = "#000000",
            ShowProgressBar = true,
            CaptionsJson = System.Text.Json.JsonSerializer.Serialize(new[]
            {
                new { start = 0.0, end = 2.5, text = moment.SuggestedHook },
                new { start = 2.5, end = Math.Max(3.0, (endTime - startTime)), text = moment.TranscriptSnippet }
            }),
            Title = moment.SuggestedTitle,
            Hook = moment.SuggestedHook,
            Caption = moment.SuggestedCaption,
            Description = moment.SuggestedDescription,
            HashtagsJson = moment.SuggestedHashtagsJson,
            CallToAction = moment.SuggestedCta,
            RenderStatus = JobStatus.CREATED
        };

        _context.Clips.Add(clip);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Created Clip {ClipId} for Moment {MomentId} in Org {OrgId}", clip.Id, moment.Id, organizationId);
        return MapToDto(clip);
    }

    public async Task<ClipDto> UpdateClipAsync(Guid organizationId, Guid clipId, UpdateClipRequest request, CancellationToken ct = default)
    {
        var clip = await _context.Clips
            .FirstOrDefaultAsync(c => c.Id == clipId && c.OrganizationId == organizationId, ct);

        if (clip == null)
        {
            throw new NotFoundException(nameof(Clip), clipId);
        }

        if (request.EndTime <= request.StartTime)
        {
            throw new ValidationException("EndTime", "Clip EndTime must be strictly greater than StartTime.");
        }

        clip.StartTime = request.StartTime;
        clip.EndTime = request.EndTime;
        clip.AspectRatio = request.AspectRatio;
        clip.CaptionStyle = request.CaptionStyle;
        clip.FontName = request.FontName;
        clip.FontSize = request.FontSize;
        clip.PrimaryColorHex = request.PrimaryColorHex;
        clip.HighlightColorHex = request.HighlightColorHex;
        clip.BackgroundColorHex = request.BackgroundColorHex;
        clip.ShowProgressBar = request.ShowProgressBar;
        clip.CaptionsJson = request.CaptionsJson;
        clip.Title = request.Title;
        clip.Hook = request.Hook;
        clip.Caption = request.Caption;
        clip.Description = request.Description;
        clip.HashtagsJson = request.HashtagsJson;
        clip.CallToAction = request.CallToAction;
        clip.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return MapToDto(clip);
    }

    public async Task<ClipDto?> GetClipByIdAsync(Guid organizationId, Guid clipId, CancellationToken ct = default)
    {
        var clip = await _context.Clips
            .FirstOrDefaultAsync(c => c.Id == clipId && c.OrganizationId == organizationId, ct);

        return clip != null ? MapToDto(clip) : null;
    }

    public async Task<List<ClipDto>> GetClipsAsync(Guid organizationId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var clips = await _context.Clips
            .Where(c => c.OrganizationId == organizationId)
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return clips.Select(MapToDto).ToList();
    }

    public async Task<JobDto> QueueRenderAsync(Guid organizationId, Guid userId, Guid clipId, CancellationToken ct = default)
    {
        var clip = await _context.Clips
            .Include(c => c.Moment)
                .ThenInclude(m => m.Source)
            .FirstOrDefaultAsync(c => c.Id == clipId && c.OrganizationId == organizationId, ct);

        if (clip == null)
        {
            throw new NotFoundException(nameof(Clip), clipId);
        }

        // 1. Strict Rights Assertion
        await _rightsAuthService.AssertCanEnterGenerationPipelineAsync(clip.Moment.SourceId, ct);

        // 2. Strict MediaAsset requirement: MediaAsset is the source of truth for rendering
        var mediaAsset = await _context.MediaAssets
            .Where(m => m.SourceId == clip.Moment.SourceId)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (mediaAsset == null || string.IsNullOrWhiteSpace(mediaAsset.StorageUrl))
        {
            throw new UnauthorizedMediaException(clip.Moment.SourceId,
                "Cannot render clip: An authorized media file must be uploaded before rendering.");
        }

        // 3. Cost Estimation & Credit Reservation
        decimal requiredCredits = Math.Max(15m, (decimal)Math.Ceiling(clip.DurationSeconds * 0.4));
        var wallet = await _creditWalletService.GetWalletAsync(organizationId, ct);

        if (wallet.AvailableBalance < requiredCredits)
        {
            throw new InsufficientCreditsException(requiredCredits, wallet.AvailableBalance);
        }

        var jobId = Guid.NewGuid();
        var idempotencyKey = $"render-clip-{clipId}-{DateTime.UtcNow.Ticks}";

        await _creditWalletService.ReserveCreditsAsync(organizationId, requiredCredits, jobId, idempotencyKey, ct);

        // 4. Create Job record with MediaAsset as source of truth
        var job = new Job
        {
            Id = jobId,
            OrganizationId = organizationId,
            UserId = userId,
            Type = JobType.VIDEO_RENDER,
            Status = JobStatus.QUEUED,
            ProgressPercentage = 0,
            CurrentStep = "Queued for video rendering engine",
            ReservedCredits = requiredCredits,
            IdempotencyKey = idempotencyKey,
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                ClipId = clip.Id,
                MediaAssetId = mediaAsset.Id,
                StorageKey = mediaAsset.StorageKey,
                clip.StartTime,
                clip.EndTime,
                clip.AspectRatio,
                clip.CaptionStyle
            })
        };

        _context.Jobs.Add(job);
        clip.RenderStatus = JobStatus.QUEUED;
        await _context.SaveChangesAsync(ct);

        // 5. Trigger asynchronous background render execution with isolated DI scope
        _ = Task.Run(async () =>
        {
            if (_scopeFactory != null)
            {
                using var scope = _scopeFactory.CreateScope();
                var scopedContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var scopedWalletService = scope.ServiceProvider.GetRequiredService<ICreditWalletService>();
                var scopedVideoProcessor = scope.ServiceProvider.GetRequiredService<IVideoProcessor>();

                try
                {
                    var bgJob = await scopedContext.Jobs.FirstOrDefaultAsync(j => j.Id == jobId);
                    var bgClip = await scopedContext.Clips.Include(c => c.Moment).ThenInclude(m => m.Source).FirstOrDefaultAsync(c => c.Id == clipId);

                    if (bgJob != null && bgClip != null)
                    {
                        bgJob.Status = JobStatus.PROCESSING;
                        bgJob.StartedAt = DateTime.UtcNow;
                        bgJob.ProgressPercentage = 25;
                        bgJob.CurrentStep = "Extracting video frames and applying 9:16 smart crop";
                        await scopedContext.SaveChangesAsync();

                        var captionsList = new List<TranscriptChunkResult>
                        {
                            new(0, bgClip.StartTime, bgClip.EndTime, bgClip.Hook, bgClip.Moment.Speaker, 0.98)
                        };

                        var bgMediaAsset = await scopedContext.MediaAssets
                            .Where(m => m.SourceId == bgClip.Moment.SourceId)
                            .OrderByDescending(m => m.CreatedAt)
                            .FirstOrDefaultAsync();

                        if (bgMediaAsset == null || string.IsNullOrWhiteSpace(bgMediaAsset.StorageUrl))
                        {
                            throw new UnauthorizedMediaException(bgClip.Moment.SourceId,
                                "Cannot render clip: No authorized media file found for this source. Discovered sources require uploaded media.");
                        }

                        var sourceVideoUrl = bgMediaAsset.StorageUrl;
                        if (!string.IsNullOrEmpty(sourceVideoUrl) && sourceVideoUrl.StartsWith("/storage/"))
                        {
                            var relative = sourceVideoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                            var localPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relative);
                            if (File.Exists(localPath)) sourceVideoUrl = localPath;
                        }

                        // Strictly ensure external YouTube or unverified video platform URLs are blocked
                        if (sourceVideoUrl.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) || sourceVideoUrl.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new UnauthorizedMediaException(bgClip.Moment.SourceId,
                                "Direct rendering from external video platforms is not permitted. An authorized media file must be uploaded.");
                        }

                        var renderReq = new RenderVideoRequest(
                            bgClip.Id,
                            sourceVideoUrl,
                            bgClip.StartTime,
                            bgClip.EndTime,
                            bgClip.AspectRatio,
                            bgClip.ResolutionWidth,
                            bgClip.ResolutionHeight,
                            bgClip.HasWatermark,
                            bgClip.CaptionStyle,
                            bgClip.FontName,
                            bgClip.FontSize,
                            bgClip.PrimaryColorHex,
                            bgClip.HighlightColorHex,
                            bgClip.BackgroundColorHex,
                            null,
                            bgClip.ShowProgressBar,
                            captionsList
                        );

                        var renderResult = await scopedVideoProcessor.RenderClipAsync(renderReq, progress =>
                        {
                            bgJob.ProgressPercentage = progress;
                        });

                        if (renderResult.Success)
                        {
                            bgClip.RenderedVideoStorageKey = renderResult.StorageKey;
                            bgClip.RenderedVideoUrl = renderResult.StorageUrl;
                            bgClip.ThumbnailUrl = renderResult.ThumbnailUrl;
                            bgClip.RenderStatus = JobStatus.COMPLETED;

                            bgJob.Status = JobStatus.COMPLETED;
                            bgJob.ProgressPercentage = 100;
                            bgJob.CurrentStep = "Render completed successfully";
                            bgJob.CompletedAt = DateTime.UtcNow;
                            bgJob.ActualCreditsConsumed = requiredCredits;
                            bgJob.IsCreditFinalized = true;

                            await scopedContext.SaveChangesAsync();
                            await scopedWalletService.CommitReservedCreditsAsync(organizationId, jobId, requiredCredits, $"Video Render for Clip {bgClip.Id}");
                        }
                        else
                        {
                            throw new Exception(renderResult.ErrorMessage ?? "Rendering failed.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to render clip {ClipId} in job {JobId}", clipId, jobId);
                    var bgJob = await scopedContext.Jobs.FirstOrDefaultAsync(j => j.Id == jobId);
                    var bgClip = await scopedContext.Clips.FirstOrDefaultAsync(c => c.Id == clipId);

                    if (bgClip != null) bgClip.RenderStatus = JobStatus.FAILED;
                    if (bgJob != null)
                    {
                        bgJob.Status = JobStatus.FAILED;
                        bgJob.ErrorMessage = ex.Message;
                        bgJob.CurrentStep = "Failed during rendering";
                    }
                    await scopedContext.SaveChangesAsync();
                    await scopedWalletService.RefundReservedCreditsAsync(organizationId, jobId, $"Render failure: {ex.Message}");
                }
            }
            else
            {
                // Fallback for isolated test runs without DI scope factory
                try
                {
                    job.Status = JobStatus.PROCESSING;
                    job.StartedAt = DateTime.UtcNow;
                    job.ProgressPercentage = 25;
                    job.CurrentStep = "Extracting video frames and applying 9:16 smart crop";
                    await _context.SaveChangesAsync();

                    var captionsList = new List<TranscriptChunkResult>
                    {
                        new(0, clip.StartTime, clip.EndTime, clip.Hook, clip.Moment.Speaker, 0.98)
                    };

                    var fallbackAsset = await _context.MediaAssets
                        .Where(m => m.SourceId == clip.Moment.SourceId)
                        .OrderByDescending(m => m.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (fallbackAsset == null || string.IsNullOrWhiteSpace(fallbackAsset.StorageUrl))
                    {
                        throw new UnauthorizedMediaException(clip.Moment.SourceId,
                            "Cannot render clip: No authorized media file found for this source. Discovered sources require uploaded media.");
                    }

                    var fallbackVideoUrl = fallbackAsset.StorageUrl;
                    if (!string.IsNullOrEmpty(fallbackVideoUrl) && fallbackVideoUrl.StartsWith("/storage/"))
                    {
                        var relative = fallbackVideoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                        var localPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relative);
                        if (File.Exists(localPath)) fallbackVideoUrl = localPath;
                    }

                    if (fallbackVideoUrl.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) || fallbackVideoUrl.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new UnauthorizedMediaException(clip.Moment.SourceId,
                            "Direct rendering from external video platforms is not permitted. An authorized media file must be uploaded.");
                    }

                    var renderReq = new RenderVideoRequest(
                        clip.Id,
                        fallbackVideoUrl,
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

                    var renderResult = await _videoProcessor.RenderClipAsync(renderReq, progress =>
                    {
                        job.ProgressPercentage = progress;
                    });

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
                        job.ActualCreditsConsumed = requiredCredits;
                        job.IsCreditFinalized = true;

                        await _context.SaveChangesAsync();
                        await _creditWalletService.CommitReservedCreditsAsync(organizationId, jobId, requiredCredits, $"Video Render for Clip {clip.Id}");
                    }
                    else
                    {
                        throw new Exception(renderResult.ErrorMessage ?? "Rendering failed.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to render clip {ClipId} in job {JobId}", clipId, jobId);
                    clip.RenderStatus = JobStatus.FAILED;
                    job.Status = JobStatus.FAILED;
                    job.ErrorMessage = ex.Message;
                    job.CurrentStep = "Failed during rendering";
                    await _context.SaveChangesAsync();
                    await _creditWalletService.RefundReservedCreditsAsync(organizationId, jobId, $"Render failure: {ex.Message}");
                }
            }
        });

        return new JobDto(
            job.Id,
            job.Type,
            job.Status,
            job.ProgressPercentage,
            job.CurrentStep,
            job.ReservedCredits,
            job.ActualCreditsConsumed,
            job.RetryCount,
            job.ErrorMessage,
            job.CreatedAt,
            job.StartedAt,
            job.CompletedAt
        );
    }

    private static ClipDto MapToDto(Clip c)
    {
        return new ClipDto(
            c.Id,
            c.MomentId,
            c.ProjectId,
            c.StartTime,
            c.EndTime,
            c.DurationSeconds,
            c.AspectRatio,
            c.ResolutionWidth,
            c.ResolutionHeight,
            c.HasWatermark,
            c.CaptionStyle,
            c.FontName,
            c.FontSize,
            c.PrimaryColorHex,
            c.HighlightColorHex,
            c.BackgroundColorHex,
            c.ShowProgressBar,
            c.CaptionsJson,
            c.Title,
            c.Hook,
            c.Caption,
            c.Description,
            c.HashtagsJson,
            c.CallToAction,
            c.RenderedVideoUrl,
            c.ThumbnailUrl,
            c.RenderStatus,
            c.CreatedAt
        );
    }
}
