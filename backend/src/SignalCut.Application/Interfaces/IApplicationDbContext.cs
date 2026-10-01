using Microsoft.EntityFrameworkCore;
using SignalCut.Domain.Entities;

namespace SignalCut.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Organization> Organizations { get; }
    DbSet<Project> Projects { get; }
    DbSet<Search> Searches { get; }
    DbSet<Source> Sources { get; }
    DbSet<SourceItem> SourceItems { get; }
    DbSet<Transcript> Transcripts { get; }
    DbSet<TranscriptChunk> TranscriptChunks { get; }
    DbSet<Moment> Moments { get; }
    DbSet<MediaAsset> MediaAssets { get; }
    DbSet<Clip> Clips { get; }
    DbSet<Job> Jobs { get; }
    DbSet<CreditWallet> CreditWallets { get; }
    DbSet<CreditTransaction> CreditTransactions { get; }
    DbSet<Payment> Payments { get; }
    DbSet<PublishingAccount> PublishingAccounts { get; }
    DbSet<PublishingJob> PublishingJobs { get; }
    DbSet<UsageRecord> UsageRecords { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
