using Microsoft.EntityFrameworkCore;
using SignalCut.Application.Interfaces;
using SignalCut.Domain.Common;
using SignalCut.Domain.Entities;
using SignalCut.Domain.Enums;

namespace SignalCut.Infrastructure.Persistence;

public class SignalCutDbContext : DbContext, IApplicationDbContext
{
    public SignalCutDbContext(DbContextOptions<SignalCutDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Search> Searches => Set<Search>();
    public DbSet<Source> Sources => Set<Source>();
    public DbSet<SourceItem> SourceItems => Set<SourceItem>();
    public DbSet<Transcript> Transcripts => Set<Transcript>();
    public DbSet<TranscriptChunk> TranscriptChunks => Set<TranscriptChunk>();
    public DbSet<Moment> Moments => Set<Moment>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<Clip> Clips => Set<Clip>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<CreditWallet> CreditWallets => Set<CreditWallet>();
    public DbSet<CreditTransaction> CreditTransactions => Set<CreditTransaction>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PublishingAccount> PublishingAccounts => Set<PublishingAccount>();
    public DbSet<PublishingJob> PublishingJobs => Set<PublishingJob>();
    public DbSet<UsageRecord> UsageRecords => Set<UsageRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global Query Filters for Soft Deletion
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Organization>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Project>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Source>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Clip>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Job>().HasQueryFilter(e => !e.IsDeleted);

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Role).HasConversion<string>();
            entity.HasOne(e => e.Organization)
                  .WithMany(o => o.Users)
                  .HasForeignKey(e => e.OrganizationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Organization Configuration
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(128);
        });

        // Credit Wallet & Ledger
        modelBuilder.Entity<CreditWallet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrganizationId).IsUnique();
            entity.Property(e => e.Balance).HasPrecision(18, 4);
            entity.Property(e => e.ReservedBalance).HasPrecision(18, 4);
            entity.Property(e => e.LifetimeEarned).HasPrecision(18, 4);
            entity.Property(e => e.LifetimeSpent).HasPrecision(18, 4);
            entity.HasOne(e => e.Organization)
                  .WithOne(o => o.CreditWallet)
                  .HasForeignKey<CreditWallet>(e => e.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CreditTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.WalletId);
            entity.HasIndex(e => e.IdempotencyKey);
            entity.Property(e => e.Type).HasConversion<string>();
            entity.Property(e => e.Amount).HasPrecision(18, 4);
            entity.Property(e => e.BalanceAfter).HasPrecision(18, 4);
            entity.Property(e => e.ReservedBalanceAfter).HasPrecision(18, 4);
            entity.HasOne(e => e.Wallet)
                  .WithMany(w => w.Transactions)
                  .HasForeignKey(e => e.WalletId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Search & Source
        modelBuilder.Entity<Search>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<Source>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.OrganizationId, e.ExternalSourceId });
            entity.Property(e => e.RightsStatus).HasConversion<string>();
            entity.Property(e => e.AuthorizationStatus).HasConversion<string>();
            entity.Property(e => e.ContentType).HasConversion<string>();
            entity.HasOne(e => e.Transcript)
                  .WithOne(t => t.Source)
                  .HasForeignKey<Transcript>(t => t.SourceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TranscriptChunk>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TranscriptId, e.ChunkIndex });
            entity.HasOne(e => e.Transcript)
                  .WithMany(t => t.Chunks)
                  .HasForeignKey(e => e.TranscriptId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Moment & Clip
        modelBuilder.Entity<Moment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SourceId);
            entity.HasIndex(e => e.OrganizationId);
            entity.Property(e => e.Objective).HasConversion<string>();
            entity.HasOne(e => e.Source)
                  .WithMany(s => s.Moments)
                  .HasForeignKey(e => e.SourceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Clip>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrganizationId);
            entity.Property(e => e.RenderStatus).HasConversion<string>();
            entity.HasOne(e => e.Moment)
                  .WithMany(m => m.Clips)
                  .HasForeignKey(e => e.MomentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Job
        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.Status);
            entity.Property(e => e.Type).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.ReservedCredits).HasPrecision(18, 4);
            entity.Property(e => e.ActualCreditsConsumed).HasPrecision(18, 4);
        });

        // Payment
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.IdempotencyKey);
            entity.Property(e => e.Provider).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.Amount).HasPrecision(18, 4);
            entity.Property(e => e.CreditsPurchased).HasPrecision(18, 4);
        });

        // Publishing
        modelBuilder.Entity<PublishingAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.OrganizationId, e.Platform });
            entity.Property(e => e.Platform).HasConversion<string>();
        });

        modelBuilder.Entity<PublishingJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrganizationId);
            entity.Property(e => e.Status).HasConversion<string>();
        });

        // Usage & Audit
        modelBuilder.Entity<UsageRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.OrganizationId, e.CreatedAt });
            entity.Property(e => e.EstimatedCost).HasPrecision(18, 4);
            entity.Property(e => e.ActualCost).HasPrecision(18, 4);
            entity.Property(e => e.CreditsDeducted).HasPrecision(18, 4);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.OrganizationId, e.CreatedAt });
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
