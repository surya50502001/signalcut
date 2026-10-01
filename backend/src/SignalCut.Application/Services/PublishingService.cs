using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SignalCut.Application.Common;
using SignalCut.Application.DTOs;
using SignalCut.Application.Interfaces;
using SignalCut.Domain.Entities;
using SignalCut.Domain.Enums;

namespace SignalCut.Application.Services;

public class PublishingService : IPublishingService
{
    private readonly IApplicationDbContext _context;
    private readonly IEnumerable<IPublishingProvider> _providers;
    private readonly ILogger<PublishingService> _logger;

    public PublishingService(
        IApplicationDbContext context,
        IEnumerable<IPublishingProvider> providers,
        ILogger<PublishingService> logger)
    {
        _context = context;
        _providers = providers;
        _logger = logger;
    }

    public async Task<List<PublishingAccountDto>> GetConnectedAccountsAsync(Guid organizationId, CancellationToken ct = default)
    {
        var accounts = await _context.PublishingAccounts
            .Where(a => a.OrganizationId == organizationId)
            .OrderBy(a => a.Platform)
            .Select(a => new PublishingAccountDto(
                a.Id,
                a.Platform,
                a.AccountName,
                a.AccountIdentifier,
                a.IsConnected,
                a.TokenExpiresAt
                // Notice: EncryptedAccessToken is NEVER projected or returned here!
            ))
            .ToListAsync(ct);

        return accounts;
    }

    public async Task<PublishingAccountDto> ConnectAccountMockAsync(Guid organizationId, PublishingPlatform platform, string accountName, CancellationToken ct = default)
    {
        var existing = await _context.PublishingAccounts
            .FirstOrDefaultAsync(a => a.OrganizationId == organizationId && a.Platform == platform, ct);

        if (existing != null)
        {
            existing.AccountName = accountName;
            existing.IsConnected = true;
            existing.EncryptedAccessToken = "enc_" + Guid.NewGuid().ToString("N");
            existing.TokenExpiresAt = DateTime.UtcNow.AddDays(60);
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return new PublishingAccountDto(existing.Id, existing.Platform, existing.AccountName, existing.AccountIdentifier, existing.IsConnected, existing.TokenExpiresAt);
        }

        var newAccount = new PublishingAccount
        {
            OrganizationId = organizationId,
            Platform = platform,
            AccountName = accountName,
            AccountIdentifier = $"@{accountName.ToLower().Replace(" ", "_")}",
            EncryptedAccessToken = "enc_" + Guid.NewGuid().ToString("N"),
            EncryptedRefreshToken = "enc_ref_" + Guid.NewGuid().ToString("N"),
            TokenExpiresAt = DateTime.UtcNow.AddDays(60),
            IsConnected = true
        };

        _context.PublishingAccounts.Add(newAccount);
        await _context.SaveChangesAsync(ct);

        return new PublishingAccountDto(newAccount.Id, newAccount.Platform, newAccount.AccountName, newAccount.AccountIdentifier, newAccount.IsConnected, newAccount.TokenExpiresAt);
    }

    public async Task<PublishingJobDto> SchedulePublishAsync(Guid organizationId, SchedulePublishRequest request, CancellationToken ct = default)
    {
        var clip = await _context.Clips
            .FirstOrDefaultAsync(c => c.Id == request.ClipId && c.OrganizationId == organizationId, ct);
        if (clip == null) throw new NotFoundException(nameof(Clip), request.ClipId);

        var account = await _context.PublishingAccounts
            .FirstOrDefaultAsync(a => a.Id == request.PublishingAccountId && a.OrganizationId == organizationId, ct);
        if (account == null) throw new NotFoundException(nameof(PublishingAccount), request.PublishingAccountId);

        var job = new PublishingJob
        {
            OrganizationId = organizationId,
            ClipId = clip.Id,
            PublishingAccountId = account.Id,
            Title = request.Title,
            Caption = request.Caption,
            HashtagsJson = System.Text.Json.JsonSerializer.Serialize(request.Hashtags),
            ScheduledAt = request.ScheduledAt ?? DateTime.UtcNow,
            Status = request.ScheduledAt.HasValue && request.ScheduledAt > DateTime.UtcNow ? PublishingStatus.SCHEDULED : PublishingStatus.PROCESSING
        };

        _context.PublishingJobs.Add(job);
        await _context.SaveChangesAsync(ct);

        // If scheduled immediately, dispatch to provider
        if (job.Status == PublishingStatus.PROCESSING)
        {
            var provider = _providers.FirstOrDefault(p => p.Platform == account.Platform) ?? _providers.FirstOrDefault();
            if (provider != null)
            {
                var pubReq = new PublishPostRequest(
                    account.Platform,
                    account.EncryptedAccessToken,
                    account.EncryptedRefreshToken,
                    clip.RenderedVideoUrl ?? "https://cdn.signalcut.app/sample.mp4",
                    job.Title,
                    job.Caption,
                    request.Hashtags,
                    job.ScheduledAt
                );

                var res = await provider.PublishAsync(pubReq, ct);
                if (res.Success)
                {
                    job.Status = PublishingStatus.PUBLISHED;
                    job.PublishedAt = DateTime.UtcNow;
                    job.ExternalPostId = res.ExternalPostId;
                    job.ExternalPostUrl = res.ExternalPostUrl;
                }
                else
                {
                    job.Status = PublishingStatus.FAILED;
                    job.ErrorMessage = res.ErrorMessage;
                }
                await _context.SaveChangesAsync(ct);
            }
        }

        return new PublishingJobDto(
            job.Id,
            job.ClipId,
            job.PublishingAccountId,
            account.Platform,
            job.Status,
            job.ScheduledAt,
            job.PublishedAt,
            job.Title,
            job.ExternalPostUrl,
            job.ErrorMessage
        );
    }

    public async Task<List<PublishingJobDto>> GetPublishingHistoryAsync(Guid organizationId, CancellationToken ct = default)
    {
        var list = await _context.PublishingJobs
            .Include(j => j.PublishingAccount)
            .Where(j => j.OrganizationId == organizationId)
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => new PublishingJobDto(
                j.Id,
                j.ClipId,
                j.PublishingAccountId,
                j.PublishingAccount.Platform,
                j.Status,
                j.ScheduledAt,
                j.PublishedAt,
                j.Title,
                j.ExternalPostUrl,
                j.ErrorMessage
            ))
            .ToListAsync(ct);

        return list;
    }
}
