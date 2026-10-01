using Microsoft.EntityFrameworkCore;
using SignalCut.Application.Common;
using SignalCut.Application.DTOs;
using SignalCut.Application.Interfaces;
using SignalCut.Domain.Enums;

namespace SignalCut.Application.Services;

public class AnalyticsAndAdminService : IAdminService, IAnalyticsService
{
    private readonly IApplicationDbContext _context;
    private readonly ICreditWalletService _creditWalletService;

    public AnalyticsAndAdminService(IApplicationDbContext context, ICreditWalletService creditWalletService)
    {
        _context = context;
        _creditWalletService = creditWalletService;
    }

    public async Task<UserAnalyticsDto> GetUserAnalyticsAsync(Guid organizationId, CancellationToken ct = default)
    {
        var searches = await _context.Searches.CountAsync(s => s.OrganizationId == organizationId, ct);
        var sources = await _context.Sources.CountAsync(s => s.OrganizationId == organizationId, ct);
        var clips = await _context.Clips.CountAsync(c => c.OrganizationId == organizationId, ct);
        var published = await _context.PublishingJobs.CountAsync(p => p.OrganizationId == organizationId && p.Status == PublishingStatus.PUBLISHED, ct);

        var wallet = await _context.CreditWallets.FirstOrDefaultAsync(w => w.OrganizationId == organizationId, ct);
        var creditsConsumed = wallet?.LifetimeSpent ?? 0;

        // Estimated human time saved: 45 minutes per generated clip
        decimal timeSavedHours = Math.Round((decimal)clips * 0.75m, 1);

        return new UserAnalyticsDto(
            searches,
            sources,
            clips,
            published,
            creditsConsumed,
            timeSavedHours
        );
    }

    public async Task<AdminDashboardMetricsDto> GetDashboardMetricsAsync(CancellationToken ct = default)
    {
        var totalUsers = await _context.Users.CountAsync(ct);
        var totalOrgs = await _context.Organizations.CountAsync(ct);
        var totalRevenue = await _context.Payments
            .Where(p => p.Status == PaymentStatus.SUCCEEDED)
            .SumAsync(p => p.Amount, ct);

        var totalCreditsIssued = await _context.CreditWallets.SumAsync(w => w.LifetimeEarned, ct);
        var totalCreditsConsumed = await _context.CreditWallets.SumAsync(w => w.LifetimeSpent, ct);

        var totalJobs = await _context.Jobs.CountAsync(ct);
        var failedJobs = await _context.Jobs.CountAsync(j => j.Status == JobStatus.FAILED, ct);
        var clipsRendered = await _context.Clips.CountAsync(c => c.RenderStatus == JobStatus.COMPLETED, ct);

        var estimatedAiCost = await _context.UsageRecords.SumAsync(u => u.ActualCost, ct);
        var grossMargin = totalRevenue > 0 ? Math.Round(((totalRevenue - estimatedAiCost) / totalRevenue) * 100, 1) : 85.0m;

        return new AdminDashboardMetricsDto(
            totalUsers,
            totalOrgs,
            totalRevenue,
            totalCreditsIssued,
            totalCreditsConsumed,
            totalJobs,
            failedJobs,
            clipsRendered,
            estimatedAiCost,
            grossMargin
        );
    }

    public async Task<List<WalletDto>> GetAllWalletsAsync(CancellationToken ct = default)
    {
        var wallets = await _context.CreditWallets
            .OrderByDescending(w => w.Balance)
            .Select(w => new WalletDto(
                w.OrganizationId,
                w.Balance,
                w.ReservedBalance,
                w.AvailableBalance,
                w.LifetimeEarned,
                w.LifetimeSpent,
                w.Currency
            ))
            .ToListAsync(ct);

        return wallets;
    }

    public async Task<bool> AdjustCreditsAsync(AdjustCreditRequest request, CancellationToken ct = default)
    {
        var type = request.Amount >= 0 ? CreditTransactionType.ADMIN_ADJUSTMENT : CreditTransactionType.ADMIN_ADJUSTMENT;

        if (request.Amount >= 0)
        {
            await _creditWalletService.AddCreditsAsync(
                request.OrganizationId,
                request.Amount,
                type,
                null,
                $"Admin Adjustment: {request.Reason}",
                $"admin-adj-{Guid.NewGuid()}",
                ct
            );
        }
        else
        {
            var wallet = await _context.CreditWallets.FirstOrDefaultAsync(w => w.OrganizationId == request.OrganizationId, ct);
            if (wallet == null) throw new NotFoundException("Wallet", request.OrganizationId);

            var absAmount = Math.Abs(request.Amount);
            if (wallet.AvailableBalance < absAmount)
            {
                throw new ValidationException("Amount", $"Cannot reduce credits below zero. Available is {wallet.AvailableBalance}");
            }

            wallet.Balance -= absAmount;
            wallet.UpdatedAt = DateTime.UtcNow;

            _context.CreditTransactions.Add(new Domain.Entities.CreditTransaction
            {
                WalletId = wallet.Id,
                Type = CreditTransactionType.ADMIN_ADJUSTMENT,
                Amount = -absAmount,
                BalanceAfter = wallet.Balance,
                ReservedBalanceAfter = wallet.ReservedBalance,
                Description = $"Admin credit deduction: {request.Reason}",
                IdempotencyKey = $"admin-deduct-{Guid.NewGuid()}"
            });

            await _context.SaveChangesAsync(ct);
        }

        return true;
    }

    public async Task<List<JobDto>> GetFailedJobsAsync(CancellationToken ct = default)
    {
        var failed = await _context.Jobs
            .Where(j => j.Status == JobStatus.FAILED)
            .OrderByDescending(j => j.UpdatedAt)
            .Take(50)
            .Select(j => new JobDto(
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
            ))
            .ToListAsync(ct);

        return failed;
    }
}
