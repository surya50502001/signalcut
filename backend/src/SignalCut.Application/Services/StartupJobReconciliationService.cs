using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SignalCut.Application.Interfaces;
using SignalCut.Domain.Enums;

namespace SignalCut.Application.Services;

public class StartupJobReconciliationService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StartupJobReconciliationService> _logger;

    public StartupJobReconciliationService(IServiceScopeFactory scopeFactory, ILogger<StartupJobReconciliationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var walletService = scope.ServiceProvider.GetService<ICreditWalletService>();

            // Reconcile jobs that were actively running (PROCESSING) when the server terminated
            var staleJobs = await context.Jobs
                .Where(j => j.Status == JobStatus.PROCESSING)
                .ToListAsync(cancellationToken);

            if (staleJobs.Count > 0)
            {
                _logger.LogWarning("Found {Count} interrupted jobs from previous server run. Reconciling states...", staleJobs.Count);

                foreach (var job in staleJobs)
                {
                    job.Status = JobStatus.FAILED;
                    job.ErrorMessage = "Job was interrupted by server restart or process shutdown. Please retry.";
                    job.CurrentStep = "Interrupted by process restart";
                    job.CompletedAt = DateTime.UtcNow;

                    if (!job.IsCreditFinalized && job.ReservedCredits > 0 && walletService != null)
                    {
                        try
                        {
                            await walletService.RefundReservedCreditsAsync(
                                job.OrganizationId,
                                job.Id,
                                "Refund for job interrupted by server shutdown",
                                cancellationToken);
                        }
                        catch (Exception refundEx)
                        {
                            _logger.LogError(refundEx, "Failed to refund credits for interrupted job {JobId}", job.Id);
                        }
                    }
                }

                await context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully reconciled {Count} interrupted jobs.", staleJobs.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during startup job reconciliation.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
