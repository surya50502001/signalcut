using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SignalCut.Application.Common;
using SignalCut.Application.DTOs;
using SignalCut.Application.Interfaces;
using SignalCut.Domain.Entities;
using SignalCut.Domain.Enums;

namespace SignalCut.Application.Services;

public class JobService : IJobService
{
    private readonly IApplicationDbContext _context;
    private readonly ICreditWalletService _creditWalletService;
    private readonly ILogger<JobService> _logger;

    public JobService(
        IApplicationDbContext context,
        ICreditWalletService creditWalletService,
        ILogger<JobService> logger)
    {
        _context = context;
        _creditWalletService = creditWalletService;
        _logger = logger;
    }

    public async Task<JobDto?> GetJobStatusAsync(Guid organizationId, Guid jobId, CancellationToken ct = default)
    {
        var job = await _context.Jobs
            .FirstOrDefaultAsync(j => j.Id == jobId && j.OrganizationId == organizationId, ct);

        return job != null ? MapToDto(job) : null;
    }

    public async Task<List<JobDto>> GetRecentJobsAsync(Guid organizationId, int limit = 20, CancellationToken ct = default)
    {
        var jobs = await _context.Jobs
            .Where(j => j.OrganizationId == organizationId)
            .OrderByDescending(j => j.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

        return jobs.Select(MapToDto).ToList();
    }

    public async Task<bool> CancelJobAsync(Guid organizationId, Guid jobId, CancellationToken ct = default)
    {
        var job = await _context.Jobs
            .FirstOrDefaultAsync(j => j.Id == jobId && j.OrganizationId == organizationId, ct);

        if (job == null) throw new NotFoundException(nameof(Job), jobId);

        if (job.Status == JobStatus.COMPLETED)
        {
            throw new ValidationException("Status", "Cannot cancel a completed job.");
        }

        job.Status = JobStatus.CANCELLED;
        job.CurrentStep = "Cancelled by user";
        job.UpdatedAt = DateTime.UtcNow;

        // Refund reserved credits if not finalized
        if (!job.IsCreditFinalized && job.ReservedCredits > 0)
        {
            await _creditWalletService.RefundReservedCreditsAsync(organizationId, jobId, "Job cancelled by user", ct);
        }

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<JobDto> RetryJobAsync(Guid organizationId, Guid jobId, CancellationToken ct = default)
    {
        var job = await _context.Jobs
            .FirstOrDefaultAsync(j => j.Id == jobId && j.OrganizationId == organizationId, ct);

        if (job == null) throw new NotFoundException(nameof(Job), jobId);

        if (job.Status != JobStatus.FAILED && job.Status != JobStatus.CANCELLED)
        {
            throw new ValidationException("Status", "Only failed or cancelled jobs can be retried.");
        }

        if (job.RetryCount >= job.MaxRetries)
        {
            throw new ValidationException("RetryCount", $"Job has reached the maximum retry limit of {job.MaxRetries}.");
        }

        job.RetryCount++;
        job.Status = JobStatus.QUEUED;
        job.ProgressPercentage = 0;
        job.ErrorMessage = null;
        job.CurrentStep = $"Retry attempt #{job.RetryCount}";
        job.UpdatedAt = DateTime.UtcNow;

        // If previous failure released reservation, re-reserve without double-charging
        if (job.ReservedCredits > 0 && !job.IsCreditFinalized)
        {
            await _creditWalletService.ReserveCreditsAsync(organizationId, job.ReservedCredits, job.Id, $"retry-{job.Id}-{job.RetryCount}", ct);
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Job {JobId} queued for retry #{Count}", jobId, job.RetryCount);
        return MapToDto(job);
    }

    private static JobDto MapToDto(Job j)
    {
        return new JobDto(
            j.Id,
            j.Type,
            j.Status,
            j.ProgressPercentage,
            j.CurrentStep,
            j.ReservedCredits,
            j.ActualCreditsConsumed,
            j.RetryCount,
            j.ErrorMessage,
            j.CreatedAt,
            j.StartedAt,
            j.CompletedAt
        );
    }
}
