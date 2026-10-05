namespace SignalCut.Domain.Enums;

public enum RightsStatus
{
    UNKNOWN,
    USER_OWNED,
    USER_AUTHORIZED,
    LICENSED,
    PUBLIC_DOMAIN,
    DISCOVERY_ONLY,
    BLOCKED
}

public enum AuthorizationStatus
{
    PENDING,
    VERIFIED,
    REJECTED,
    EXPIRED,
    NOT_REQUIRED
}

public enum ContentType
{
    VIDEO,
    PODCAST,
    CONFERENCE,
    INTERVIEW,
    WEB_VIDEO,
    USER_UPLOAD
}

public enum MomentObjective
{
    Educational,
    Newsworthy,
    Emotional,
    Controversial,
    Inspirational,
    Technical,
    Promotional
}

public enum JobStatus
{
    CREATED,
    QUEUED,
    PROCESSING,
    WAITING,
    COMPLETED,
    FAILED,
    CANCELLED
}

public enum JobType
{
    SEARCH_DISCOVERY,
    TRANSCRIPT_PROCESSING,
    AI_ANALYSIS,
    MOMENT_DETECTION,
    CLIP_GENERATION,
    CAPTION_GENERATION,
    VIDEO_RENDER,
    PUBLISHING
}

public enum CreditTransactionType
{
    PURCHASE,
    RESERVATION,
    CONSUMPTION,
    REFUND,
    BONUS,
    ADMIN_ADJUSTMENT,
    EXPIRATION
}

public enum PaymentStatus
{
    PENDING,
    SUCCEEDED,
    FAILED,
    REFUNDED
}

public enum PaymentProviderType
{
    STRIPE,
    RAZORPAY,
    MOCK
}

public enum PublishingPlatform
{
    YOUTUBE,
    INSTAGRAM,
    TIKTOK,
    LINKEDIN,
    X
}

public enum PublishingStatus
{
    DRAFT,
    SCHEDULED,
    PROCESSING,
    PUBLISHED,
    FAILED,
    CANCELLED
}

public enum UserRole
{
    USER,
    ADMIN,
    SUPERADMIN
}
