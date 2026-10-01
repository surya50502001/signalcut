using SignalCut.Domain.Enums;

namespace SignalCut.Application.DTOs;

// --- AUTH DTOs ---
public record RegisterRequest(string Email, string Password, string FullName, string? OrganizationName);
public record LoginRequest(string Email, string Password);
public record RefreshTokenRequest(string RefreshToken);
public record AuthResponse(string Token, string RefreshToken, DateTime ExpiresAt, UserDto User, OrganizationDto Organization);
public record UserDto(Guid Id, string Email, string FullName, UserRole Role, bool IsEmailVerified, bool HasUsedFreeTrial);
public record OrganizationDto(Guid Id, string Name, string Slug, decimal AvailableCredits);

// --- SEARCH & DISCOVERY DTOs ---
public record SearchQueryRequest(string Query, Guid? ProjectId, int Limit = 20);
public record SearchResultDto(
    Guid SearchId,
    string Query,
    List<string> ParsedTopics,
    string Intent,
    List<SourceDto> Sources
);
public record SourceDto(
    Guid Id,
    string ExternalSourceId,
    string Provider,
    string Title,
    string Description,
    string Creator,
    string Url,
    DateTime? PublishedAt,
    double DurationSeconds,
    string ThumbnailUrl,
    string Language,
    ContentType ContentType,
    RightsStatus RightsStatus,
    AuthorizationStatus AuthorizationStatus,
    bool TranscriptAvailability,
    bool IsAuthorizedForGeneration,
    double RelevanceScore
);

// --- RIGHTS CONFIRMATION DTO (CRITICAL) ---
public record RightsConfirmationRequest(
    Guid SourceId,
    RightsStatus ClaimedRightsStatus, // USER_OWNED, USER_AUTHORIZED, LICENSED, PUBLIC_DOMAIN
    string ConfirmationStatement, // "I confirm that I own or have permission to use this content."
    string? ProofDocumentUrl
);
public record RightsConfirmationResponse(
    bool Success,
    Guid SourceId,
    RightsStatus RightsStatus,
    AuthorizationStatus AuthorizationStatus,
    DateTime ConfirmedAt,
    string Message
);

// --- MOMENTS DTOs ---
public record MomentDto(
    Guid Id,
    Guid SourceId,
    double StartTime,
    double EndTime,
    double DurationSeconds,
    string TranscriptSnippet,
    string Reason,
    double TopicRelevance,
    double HookStrength,
    double InformationDensity,
    double ClipScore,
    string Speaker,
    MomentObjective Objective,
    string SuggestedHook,
    string SuggestedTitle,
    string SuggestedCaption,
    string SuggestedDescription,
    List<string> SuggestedHashtags,
    string SuggestedCta
);

// --- CLIPS DTOs ---
public record CreateClipRequest(
    Guid MomentId,
    Guid? ProjectId,
    double? StartTime,
    double? EndTime,
    string AspectRatio = "9:16",
    string CaptionStyle = "TIKTOK_POP"
);

public record UpdateClipRequest(
    double StartTime,
    double EndTime,
    string AspectRatio,
    string CaptionStyle,
    string FontName,
    int FontSize,
    string PrimaryColorHex,
    string HighlightColorHex,
    string BackgroundColorHex,
    bool ShowProgressBar,
    string CaptionsJson,
    string Title,
    string Hook,
    string Caption,
    string Description,
    string HashtagsJson,
    string CallToAction
);

public record ClipDto(
    Guid Id,
    Guid MomentId,
    Guid? ProjectId,
    double StartTime,
    double EndTime,
    double DurationSeconds,
    string AspectRatio,
    int ResolutionWidth,
    int ResolutionHeight,
    bool HasWatermark,
    string CaptionStyle,
    string FontName,
    int FontSize,
    string PrimaryColorHex,
    string HighlightColorHex,
    string BackgroundColorHex,
    bool ShowProgressBar,
    string CaptionsJson,
    string Title,
    string Hook,
    string Caption,
    string Description,
    string HashtagsJson,
    string CallToAction,
    string? RenderedVideoUrl,
    string? ThumbnailUrl,
    JobStatus RenderStatus,
    DateTime CreatedAt
);

// --- CREDIT & BILLING DTOs ---
public record WalletDto(
    Guid OrganizationId,
    decimal Balance,
    decimal ReservedBalance,
    decimal AvailableBalance,
    decimal LifetimeEarned,
    decimal LifetimeSpent,
    string Currency
);

public record TransactionDto(
    Guid Id,
    CreditTransactionType Type,
    decimal Amount,
    decimal BalanceAfter,
    decimal ReservedBalanceAfter,
    Guid? ReferenceJobId,
    Guid? ReferencePaymentId,
    string Description,
    string? IdempotencyKey,
    DateTime CreatedAt
);

public record CreditPackageDto(
    string Id,
    string Name,
    decimal Price,
    string Currency,
    decimal Credits,
    decimal BonusCredits,
    string Description,
    bool IsPopular
);

public record CostEstimationDto(
    decimal RequiredCredits,
    decimal AvailableCredits,
    bool HasSufficientCredits,
    string Operation,
    string Breakdown
);

// --- JOBS DTOs ---
public record JobDto(
    Guid Id,
    JobType Type,
    JobStatus Status,
    int ProgressPercentage,
    string? CurrentStep,
    decimal ReservedCredits,
    decimal ActualCreditsConsumed,
    int RetryCount,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt
);

// --- PUBLISHING DTOs ---
public record PublishingAccountDto(
    Guid Id,
    PublishingPlatform Platform,
    string AccountName,
    string AccountIdentifier,
    bool IsConnected,
    DateTime? TokenExpiresAt
);

public record SchedulePublishRequest(
    Guid ClipId,
    Guid PublishingAccountId,
    string Title,
    string Caption,
    List<string> Hashtags,
    DateTime? ScheduledAt
);

public record PublishingJobDto(
    Guid Id,
    Guid ClipId,
    Guid PublishingAccountId,
    PublishingPlatform Platform,
    PublishingStatus Status,
    DateTime? ScheduledAt,
    DateTime? PublishedAt,
    string Title,
    string? ExternalPostUrl,
    string? ErrorMessage
);

// --- ADMIN & ANALYTICS DTOs ---
public record AdminDashboardMetricsDto(
    int TotalUsers,
    int TotalOrganizations,
    decimal TotalRevenue,
    decimal TotalCreditsIssued,
    decimal TotalCreditsConsumed,
    int TotalJobsProcessed,
    int FailedJobsCount,
    int TotalClipsRendered,
    decimal EstimatedAiCosts,
    decimal GrossMargin
);

public record AdjustCreditRequest(
    Guid OrganizationId,
    decimal Amount,
    string Reason
);

public record UserAnalyticsDto(
    int TotalSearches,
    int TotalSourcesDiscovered,
    int TotalClipsGenerated,
    int TotalPublished,
    decimal TotalCreditsConsumed,
    decimal EstimatedTimeSavedHours
);
