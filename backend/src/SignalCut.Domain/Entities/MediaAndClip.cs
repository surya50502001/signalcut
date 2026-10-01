using SignalCut.Domain.Common;
using SignalCut.Domain.Enums;

namespace SignalCut.Domain.Entities;

public class MediaAsset : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid? SourceId { get; set; }
    public string AssetType { get; set; } = "SOURCE_VIDEO"; // SOURCE_VIDEO, EXTRACTED_AUDIO, BROLL, LOGO
    public string StorageKey { get; set; } = string.Empty;
    public string StorageUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = "video/mp4";
    public long FileSizeBytes { get; set; }
    public double DurationSeconds { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    // Navigation
    public virtual Organization Organization { get; set; } = null!;
    public virtual Source? Source { get; set; }
}

public class Clip : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid MomentId { get; set; }
    public Guid CreatedByUserId { get; set; }

    // Video Trims & Timing
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public double DurationSeconds => EndTime - StartTime;

    // Formatting & Layout
    public string AspectRatio { get; set; } = "9:16"; // 9:16, 1:1, 16:9
    public int ResolutionWidth { get; set; } = 1080;
    public int ResolutionHeight { get; set; } = 1920;
    public bool HasWatermark { get; set; } = false;

    // Brand Kit Styling
    public string CaptionStyle { get; set; } = "TIKTOK_POP"; // CLASSIC, TIKTOK_POP, KARAOKE, MINIMALIST
    public string FontName { get; set; } = "Inter";
    public int FontSize { get; set; } = 42;
    public string PrimaryColorHex { get; set; } = "#FFFFFF";
    public string HighlightColorHex { get; set; } = "#FACC15";
    public string BackgroundColorHex { get; set; } = "#000000";
    public string? LogoStorageKey { get; set; }
    public bool ShowProgressBar { get; set; } = true;

    // Editable Captions and Text JSON
    public string CaptionsJson { get; set; } = "[]"; // [{ "start": 0.0, "end": 2.5, "text": "..." }]

    // AI Generated Copy (Editable by user)
    public string Title { get; set; } = string.Empty;
    public string Hook { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string HashtagsJson { get; set; } = "[]";
    public string CallToAction { get; set; } = string.Empty;

    // Output Render Asset
    public string? RenderedVideoStorageKey { get; set; }
    public string? RenderedVideoUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public JobStatus RenderStatus { get; set; } = JobStatus.CREATED;

    // Navigation
    public virtual Organization Organization { get; set; } = null!;
    public virtual Project? Project { get; set; }
    public virtual Moment Moment { get; set; } = null!;
    public virtual ICollection<PublishingJob> PublishingJobs { get; set; } = new List<PublishingJob>();
}

public class Job : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }

    public JobType Type { get; set; }
    public JobStatus Status { get; set; } = JobStatus.CREATED;
    public int ProgressPercentage { get; set; } = 0;
    public string? CurrentStep { get; set; }

    public string? IdempotencyKey { get; set; }
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    public string? ErrorMessage { get; set; }

    // Credits bound to job
    public decimal ReservedCredits { get; set; } = 0;
    public decimal ActualCreditsConsumed { get; set; } = 0;
    public bool IsCreditFinalized { get; set; } = false;

    // Payload and Result Data
    public string PayloadJson { get; set; } = "{}";
    public string? ResultJson { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public virtual Organization Organization { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
