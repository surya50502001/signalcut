using SignalCut.Domain.Common;
using SignalCut.Domain.Enums;

namespace SignalCut.Domain.Entities;

public class PublishingAccount : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public PublishingPlatform Platform { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string AccountIdentifier { get; set; } = string.Empty;

    // Encrypted OAuth tokens - NEVER returned plaintext in APIs
    public string EncryptedAccessToken { get; set; } = string.Empty;
    public string? EncryptedRefreshToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public bool IsConnected { get; set; } = true;

    // Navigation
    public virtual Organization Organization { get; set; } = null!;
    public virtual ICollection<PublishingJob> PublishingJobs { get; set; } = new List<PublishingJob>();
}

public class PublishingJob : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid ClipId { get; set; }
    public Guid PublishingAccountId { get; set; }

    public PublishingStatus Status { get; set; } = PublishingStatus.DRAFT;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? PublishedAt { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string HashtagsJson { get; set; } = "[]";

    public string? ExternalPostId { get; set; }
    public string? ExternalPostUrl { get; set; }
    public string? ErrorMessage { get; set; }

    // Navigation
    public virtual Organization Organization { get; set; } = null!;
    public virtual Clip Clip { get; set; } = null!;
    public virtual PublishingAccount PublishingAccount { get; set; } = null!;
}

public class UsageRecord : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public Guid? JobId { get; set; }

    public string Provider { get; set; } = string.Empty; // OpenAI, Anthropic, Whisper, FFmpeg, etc.
    public string Model { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty; // Transcript, MomentDetection, Copywriting, VideoRender

    public long InputUnits { get; set; } // e.g. prompt tokens or audio seconds
    public long OutputUnits { get; set; } // e.g. completion tokens or render seconds
    public decimal EstimatedCost { get; set; }
    public decimal ActualCost { get; set; }
    public decimal CreditsDeducted { get; set; }

    // Navigation
    public virtual Organization Organization { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}

public class AuditLog : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid? UserId { get; set; }

    public string Action { get; set; } = string.Empty; // e.g. "RIGHTS_CONFIRMED", "CREDIT_PURCHASED", "CLIP_RENDERED", "LOGIN"
    public string ResourceType { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    public string DetailsJson { get; set; } = "{}";

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
