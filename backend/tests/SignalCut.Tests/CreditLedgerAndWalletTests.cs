using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SignalCut.Application.Common;
using SignalCut.Application.Services;
using SignalCut.Domain.Entities;
using SignalCut.Domain.Enums;
using SignalCut.Infrastructure.Persistence;
using Xunit;

namespace SignalCut.Tests;

public class CreditLedgerAndWalletTests
{
    private SignalCutDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<SignalCutDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new SignalCutDbContext(options);
    }

    [Fact]
    public async Task CriticalTest1_UserCannotProcessWithoutSufficientCredits()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new CreditWalletService(context, NullLogger<CreditWalletService>.Instance);
        var orgId = Guid.NewGuid();

        // New wallet starts with 50 trial credits
        var wallet = await service.GetWalletAsync(orgId);
        wallet.AvailableBalance.Should().Be(50m);

        // Act & Assert: Attempting to reserve 100 credits must throw InsufficientCreditsException
        var jobId = Guid.NewGuid();
        var act = async () => await service.ReserveCreditsAsync(orgId, 100m, jobId, "test-key-1");

        var ex = await act.Should().ThrowAsync<InsufficientCreditsException>();
        ex.Which.RequiredCredits.Should().Be(100m);
        ex.Which.AvailableCredits.Should().Be(50m);

        // Wallet balance must remain untouched
        var walletAfter = await service.GetWalletAsync(orgId);
        walletAfter.Balance.Should().Be(50m);
        walletAfter.ReservedBalance.Should().Be(0m);
        walletAfter.AvailableBalance.Should().Be(50m);
    }

    [Fact]
    public async Task CriticalTest2_FailedJobRefundsReservedCredits()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new CreditWalletService(context, NullLogger<CreditWalletService>.Instance);
        var orgId = Guid.NewGuid();
        var jobId = Guid.NewGuid();

        // 1. Reserve 25 credits
        await service.ReserveCreditsAsync(orgId, 25m, jobId, "job-reserve-1");
        var walletMid = await service.GetWalletAsync(orgId);
        walletMid.ReservedBalance.Should().Be(25m);
        walletMid.AvailableBalance.Should().Be(25m); // 50 - 25

        // Act: Job fails, triggers refund
        await service.RefundReservedCreditsAsync(orgId, jobId, "Rendering worker failed");

        // Assert: Reservation released, available balance restored
        var walletAfter = await service.GetWalletAsync(orgId);
        walletAfter.ReservedBalance.Should().Be(0m);
        walletAfter.AvailableBalance.Should().Be(50m);
        walletAfter.Balance.Should().Be(50m);

        // Ledger check
        var txs = await service.GetTransactionsAsync(orgId);
        txs.Should().Contain(t => t.Type == CreditTransactionType.REFUND && t.Amount == 25m);
    }

    [Fact]
    public async Task CriticalTest3_RetryCannotDoubleCharge()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var creditService = new CreditWalletService(context, NullLogger<CreditWalletService>.Instance);
        var jobService = new JobService(context, creditService, NullLogger<JobService>.Instance);

        var org = new Organization { Id = Guid.NewGuid(), Name = "Org Test", Slug = "org-test" };
        var user = new User { Id = Guid.NewGuid(), OrganizationId = org.Id, Email = "test@signalcut.app" };
        var job = new Job
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            UserId = user.Id,
            Type = JobType.VIDEO_RENDER,
            Status = JobStatus.FAILED,
            ReservedCredits = 20m,
            IsCreditFinalized = false,
            RetryCount = 0
        };

        context.Organizations.Add(org);
        context.Users.Add(user);
        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        // Act: Retry the job
        var retried = await jobService.RetryJobAsync(org.Id, job.Id);

        retried.Status.Should().Be(JobStatus.QUEUED);
        retried.RetryCount.Should().Be(1);

        // Assert: Retrying does not increase ReservedCredits on the job
        retried.ReservedCredits.Should().Be(20m);
    }

    [Fact]
    public async Task CriticalTest7_WorkerFailureDoesNotLeaveCreditsPermanentlyLocked()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new CreditWalletService(context, NullLogger<CreditWalletService>.Instance);
        var orgId = Guid.NewGuid();
        var jobId = Guid.NewGuid();

        await service.ReserveCreditsAsync(orgId, 30m, jobId, "key-lock-test");
        var walletDuring = await service.GetWalletAsync(orgId);
        walletDuring.ReservedBalance.Should().Be(30m);

        // Act: Worker crashes or times out -> release reservation
        await service.RefundReservedCreditsAsync(orgId, jobId, "Worker crashed unexpectedly");

        // Assert: No credits locked
        var walletFinal = await service.GetWalletAsync(orgId);
        walletFinal.ReservedBalance.Should().Be(0m);
        walletFinal.AvailableBalance.Should().Be(50m);
    }

    [Fact]
    public async Task CriticalTest10_ConcurrentJobsCannotOverspendCredits()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new CreditWalletService(context, NullLogger<CreditWalletService>.Instance);
        var orgId = Guid.NewGuid(); // Has 50 initial trial credits

        // Act: Launch 5 concurrent jobs each requiring 20 credits (Total 100 > 50)
        int successCount = 0;
        int failureCount = 0;

        var tasks = Enumerable.Range(1, 5).Select(async i =>
        {
            try
            {
                var jobId = Guid.NewGuid();
                await service.ReserveCreditsAsync(orgId, 20m, jobId, $"concurrent-job-{i}");
                Interlocked.Increment(ref successCount);
            }
            catch (InsufficientCreditsException)
            {
                Interlocked.Increment(ref failureCount);
            }
        });

        await Task.WhenAll(tasks);

        // Assert: Exactly 2 jobs succeed (40 credits <= 50), 3 jobs fail due to insufficient credits
        successCount.Should().Be(2);
        failureCount.Should().Be(3);

        var finalWallet = await service.GetWalletAsync(orgId);
        finalWallet.ReservedBalance.Should().Be(40m);
        finalWallet.AvailableBalance.Should().Be(10m);
    }
}
