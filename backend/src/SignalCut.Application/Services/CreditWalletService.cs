using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SignalCut.Application.Common;
using SignalCut.Application.DTOs;
using SignalCut.Application.Interfaces;
using SignalCut.Domain.Entities;
using SignalCut.Domain.Enums;

namespace SignalCut.Application.Services;

public class CreditWalletService : ICreditWalletService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CreditWalletService> _logger;
    private static readonly SemaphoreSlim _walletLock = new(1, 1);

    public CreditWalletService(IApplicationDbContext context, ILogger<CreditWalletService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<WalletDto> GetWalletAsync(Guid organizationId, CancellationToken ct = default)
    {
        var wallet = await GetOrCreateWalletEntityAsync(organizationId, ct);
        return new WalletDto(
            wallet.OrganizationId,
            wallet.Balance,
            wallet.ReservedBalance,
            wallet.AvailableBalance,
            wallet.LifetimeEarned,
            wallet.LifetimeSpent,
            wallet.Currency
        );
    }

    public async Task<List<TransactionDto>> GetTransactionsAsync(Guid organizationId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var wallet = await GetOrCreateWalletEntityAsync(organizationId, ct);

        var transactions = await _context.CreditTransactions
            .Where(t => t.WalletId == wallet.Id)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionDto(
                t.Id,
                t.Type,
                t.Amount,
                t.BalanceAfter,
                t.ReservedBalanceAfter,
                t.ReferenceJobId,
                t.ReferencePaymentId,
                t.Description,
                t.IdempotencyKey,
                t.CreatedAt
            ))
            .ToListAsync(ct);

        return transactions;
    }

    public async Task<bool> ReserveCreditsAsync(Guid organizationId, decimal amount, Guid jobId, string idempotencyKey, CancellationToken ct = default)
    {
        if (amount <= 0) return true;

        await _walletLock.WaitAsync(ct);
        try
        {
            // Check idempotency: if already reserved for this job, return true
            if (!string.IsNullOrEmpty(idempotencyKey))
            {
                var existingTx = await _context.CreditTransactions
                    .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey && t.Type == CreditTransactionType.RESERVATION, ct);
                if (existingTx != null)
                {
                    _logger.LogInformation("Reservation with idempotency key {Key} already exists. Skipping duplicate reservation.", idempotencyKey);
                    return true;
                }
            }

            var wallet = await GetOrCreateWalletEntityAsync(organizationId, ct);

            if (wallet.AvailableBalance < amount)
            {
                _logger.LogWarning("Insufficient credits in org {OrgId}: Requested {Req}, Available {Avail}", organizationId, amount, wallet.AvailableBalance);
                throw new InsufficientCreditsException(amount, wallet.AvailableBalance);
            }

            // Increase reserved balance
            wallet.ReservedBalance += amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var transaction = new CreditTransaction
            {
                WalletId = wallet.Id,
                Type = CreditTransactionType.RESERVATION,
                Amount = amount,
                BalanceAfter = wallet.Balance,
                ReservedBalanceAfter = wallet.ReservedBalance,
                ReferenceJobId = jobId,
                Description = $"Reserved {amount} credits for job {jobId}",
                IdempotencyKey = idempotencyKey
            };

            _context.CreditTransactions.Add(transaction);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Reserved {Amount} credits for job {JobId} in org {OrgId}", amount, jobId, organizationId);
            return true;
        }
        finally
        {
            _walletLock.Release();
        }
    }

    public async Task<bool> CommitReservedCreditsAsync(Guid organizationId, Guid jobId, decimal actualAmountConsumed, string description, CancellationToken ct = default)
    {
        await _walletLock.WaitAsync(ct);
        try
        {
            var wallet = await GetOrCreateWalletEntityAsync(organizationId, ct);

            // Find reservation transaction for this job
            var reservation = await _context.CreditTransactions
                .Where(t => t.WalletId == wallet.Id && t.ReferenceJobId == jobId && t.Type == CreditTransactionType.RESERVATION)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync(ct);

            var reservedAmount = reservation?.Amount ?? 0;

            // Release the reservation
            wallet.ReservedBalance = Math.Max(0, wallet.ReservedBalance - reservedAmount);

            // Deduct actual consumed credits from Balance
            if (actualAmountConsumed > wallet.Balance)
            {
                actualAmountConsumed = wallet.Balance; // Never allow negative balance
            }

            wallet.Balance -= actualAmountConsumed;
            wallet.LifetimeSpent += actualAmountConsumed;
            wallet.UpdatedAt = DateTime.UtcNow;

            var consumptionTx = new CreditTransaction
            {
                WalletId = wallet.Id,
                Type = CreditTransactionType.CONSUMPTION,
                Amount = -actualAmountConsumed,
                BalanceAfter = wallet.Balance,
                ReservedBalanceAfter = wallet.ReservedBalance,
                ReferenceJobId = jobId,
                Description = string.IsNullOrWhiteSpace(description) ? $"Consumed {actualAmountConsumed} credits for job {jobId}" : description,
                IdempotencyKey = $"commit-{jobId}"
            };

            _context.CreditTransactions.Add(consumptionTx);

            // If actual consumption was less than reserved amount, record refund of excess
            var excessReserved = reservedAmount - actualAmountConsumed;
            if (excessReserved > 0)
            {
                var refundTx = new CreditTransaction
                {
                    WalletId = wallet.Id,
                    Type = CreditTransactionType.REFUND,
                    Amount = excessReserved,
                    BalanceAfter = wallet.Balance,
                    ReservedBalanceAfter = wallet.ReservedBalance,
                    ReferenceJobId = jobId,
                    Description = $"Released unused reservation of {excessReserved} credits for job {jobId}",
                    IdempotencyKey = $"refund-excess-{jobId}"
                };
                _context.CreditTransactions.Add(refundTx);
            }

            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Committed {Actual} credits (reserved was {Reserved}) for job {JobId}", actualAmountConsumed, reservedAmount, jobId);
            return true;
        }
        finally
        {
            _walletLock.Release();
        }
    }

    public async Task<bool> RefundReservedCreditsAsync(Guid organizationId, Guid jobId, string reason, CancellationToken ct = default)
    {
        await _walletLock.WaitAsync(ct);
        try
        {
            var wallet = await GetOrCreateWalletEntityAsync(organizationId, ct);

            // Check if already refunded
            var existingRefund = await _context.CreditTransactions
                .FirstOrDefaultAsync(t => t.WalletId == wallet.Id && t.ReferenceJobId == jobId && t.Type == CreditTransactionType.REFUND, ct);
            if (existingRefund != null)
            {
                _logger.LogInformation("Job {JobId} reservation already refunded.", jobId);
                return true;
            }

            var reservation = await _context.CreditTransactions
                .Where(t => t.WalletId == wallet.Id && t.ReferenceJobId == jobId && t.Type == CreditTransactionType.RESERVATION)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (reservation != null && reservation.Amount > 0)
            {
                wallet.ReservedBalance = Math.Max(0, wallet.ReservedBalance - reservation.Amount);
                wallet.UpdatedAt = DateTime.UtcNow;

                var refundTx = new CreditTransaction
                {
                    WalletId = wallet.Id,
                    Type = CreditTransactionType.REFUND,
                    Amount = reservation.Amount,
                    BalanceAfter = wallet.Balance,
                    ReservedBalanceAfter = wallet.ReservedBalance,
                    ReferenceJobId = jobId,
                    Description = $"Refunded {reservation.Amount} reserved credits for failed/cancelled job {jobId}. Reason: {reason}",
                    IdempotencyKey = $"refund-job-{jobId}"
                };

                _context.CreditTransactions.Add(refundTx);
                await _context.SaveChangesAsync(ct);

                _logger.LogInformation("Released {Amount} reserved credits for failed job {JobId}", reservation.Amount, jobId);
            }

            return true;
        }
        finally
        {
            _walletLock.Release();
        }
    }

    public async Task<CreditTransaction> AddCreditsAsync(Guid organizationId, decimal amount, CreditTransactionType type, Guid? referencePaymentId, string description, string? idempotencyKey, CancellationToken ct = default)
    {
        if (amount <= 0) throw new ValidationException("Amount", "Credit addition amount must be greater than zero.");

        await _walletLock.WaitAsync(ct);
        try
        {
            // Idempotency check for payments/bonus additions
            if (!string.IsNullOrEmpty(idempotencyKey))
            {
                var existing = await _context.CreditTransactions
                    .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey, ct);
                if (existing != null)
                {
                    _logger.LogInformation("Duplicate transaction skipped with key {Key}", idempotencyKey);
                    return existing;
                }
            }

            var wallet = await GetOrCreateWalletEntityAsync(organizationId, ct);
            wallet.Balance += amount;
            wallet.LifetimeEarned += amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var tx = new CreditTransaction
            {
                WalletId = wallet.Id,
                Type = type,
                Amount = amount,
                BalanceAfter = wallet.Balance,
                ReservedBalanceAfter = wallet.ReservedBalance,
                ReferencePaymentId = referencePaymentId,
                Description = description,
                IdempotencyKey = idempotencyKey
            };

            _context.CreditTransactions.Add(tx);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Added {Amount} credits (type: {Type}) to org {OrgId}. New balance: {Balance}", amount, type, organizationId, wallet.Balance);
            return tx;
        }
        finally
        {
            _walletLock.Release();
        }
    }

    public async Task<CostEstimationDto> EstimateCostAsync(Guid organizationId, string operation, double durationSeconds = 0, CancellationToken ct = default)
    {
        var wallet = await GetOrCreateWalletEntityAsync(organizationId, ct);

        decimal requiredCredits = operation.ToUpperInvariant() switch
        {
            "TRANSCRIPT" => 5m,
            "MOMENT_DETECTION" => 10m,
            "COPYWRITING" => 5m,
            "CLIP_RENDER" => durationSeconds > 0 ? Math.Max(15m, (decimal)Math.Ceiling(durationSeconds * 0.4)) : 25m,
            "FULL_PIPELINE" => 40m,
            _ => 10m
        };

        bool hasSufficient = wallet.AvailableBalance >= requiredCredits;
        string breakdown = $"Operation: {operation}. Base cost: {requiredCredits} credits. Available: {wallet.AvailableBalance} credits.";

        return new CostEstimationDto(requiredCredits, wallet.AvailableBalance, hasSufficient, operation, breakdown);
    }

    private async Task<CreditWallet> GetOrCreateWalletEntityAsync(Guid organizationId, CancellationToken ct)
    {
        var wallet = await _context.CreditWallets.FirstOrDefaultAsync(w => w.OrganizationId == organizationId, ct);
        if (wallet == null)
        {
            wallet = new CreditWallet
            {
                OrganizationId = organizationId,
                Balance = 50m, // Generous free trial initial credits for immediate exploration
                ReservedBalance = 0m,
                LifetimeEarned = 50m,
                Currency = "INR"
            };
            _context.CreditWallets.Add(wallet);
            await _context.SaveChangesAsync(ct);

            // Record initial bonus transaction in ledger
            var initialBonus = new CreditTransaction
            {
                WalletId = wallet.Id,
                Type = CreditTransactionType.BONUS,
                Amount = 50m,
                BalanceAfter = 50m,
                ReservedBalanceAfter = 0m,
                Description = "Welcome trial credits grant",
                IdempotencyKey = $"welcome-{organizationId}"
            };
            _context.CreditTransactions.Add(initialBonus);
            await _context.SaveChangesAsync(ct);
        }
        return wallet;
    }
}
