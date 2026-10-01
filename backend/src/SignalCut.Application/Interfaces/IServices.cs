using SignalCut.Application.DTOs;
using SignalCut.Domain.Entities;
using SignalCut.Domain.Enums;

namespace SignalCut.Application.Interfaces;

public interface ICurrentUserContext
{
    Guid? UserId { get; }
    Guid? OrganizationId { get; }
    string? Email { get; }
    UserRole? Role { get; }
    bool IsAuthenticated { get; }
}

public interface ICreditWalletService
{
    Task<WalletDto> GetWalletAsync(Guid organizationId, CancellationToken ct = default);
    Task<List<TransactionDto>> GetTransactionsAsync(Guid organizationId, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<bool> ReserveCreditsAsync(Guid organizationId, decimal amount, Guid jobId, string idempotencyKey, CancellationToken ct = default);
    Task<bool> CommitReservedCreditsAsync(Guid organizationId, Guid jobId, decimal actualAmountConsumed, string description, CancellationToken ct = default);
    Task<bool> RefundReservedCreditsAsync(Guid organizationId, Guid jobId, string reason, CancellationToken ct = default);
    Task<CreditTransaction> AddCreditsAsync(Guid organizationId, decimal amount, CreditTransactionType type, Guid? referencePaymentId, string description, string? idempotencyKey, CancellationToken ct = default);
    Task<CostEstimationDto> EstimateCostAsync(Guid organizationId, string operation, double durationSeconds = 0, CancellationToken ct = default);
}

public interface ISearchDiscoveryService
{
    Task<SearchResultDto> SearchTopicAsync(Guid organizationId, Guid userId, SearchQueryRequest request, CancellationToken ct = default);
    Task<SourceDto?> GetSourceByIdAsync(Guid organizationId, Guid sourceId, CancellationToken ct = default);
    Task<List<SourceDto>> GetDiscoveredSourcesAsync(Guid organizationId, int page = 1, int pageSize = 20, CancellationToken ct = default);
}

public interface IRightsAuthorizationService
{
    Task<RightsConfirmationResponse> ConfirmRightsAsync(Guid organizationId, Guid userId, RightsConfirmationRequest request, CancellationToken ct = default);
    Task<bool> AssertCanEnterGenerationPipelineAsync(Guid sourceId, CancellationToken ct = default);
}

public interface IMomentService
{
    Task<List<MomentDto>> DetectMomentsAsync(Guid organizationId, Guid userId, Guid sourceId, MomentObjective objective, CancellationToken ct = default);
    Task<List<MomentDto>> GetMomentsBySourceAsync(Guid organizationId, Guid sourceId, CancellationToken ct = default);
    Task<MomentDto?> GetMomentByIdAsync(Guid organizationId, Guid momentId, CancellationToken ct = default);
    Task<MomentDto> RegenerateCopyAsync(Guid organizationId, Guid momentId, CancellationToken ct = default);
}

public interface IClipService
{
    Task<ClipDto> CreateClipFromMomentAsync(Guid organizationId, Guid userId, CreateClipRequest request, CancellationToken ct = default);
    Task<ClipDto> UpdateClipAsync(Guid organizationId, Guid clipId, UpdateClipRequest request, CancellationToken ct = default);
    Task<ClipDto?> GetClipByIdAsync(Guid organizationId, Guid clipId, CancellationToken ct = default);
    Task<List<ClipDto>> GetClipsAsync(Guid organizationId, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<JobDto> QueueRenderAsync(Guid organizationId, Guid userId, Guid clipId, CancellationToken ct = default);
}

public interface IJobService
{
    Task<JobDto?> GetJobStatusAsync(Guid organizationId, Guid jobId, CancellationToken ct = default);
    Task<List<JobDto>> GetRecentJobsAsync(Guid organizationId, int limit = 20, CancellationToken ct = default);
    Task<bool> CancelJobAsync(Guid organizationId, Guid jobId, CancellationToken ct = default);
    Task<JobDto> RetryJobAsync(Guid organizationId, Guid jobId, CancellationToken ct = default);
}

public interface IPaymentService
{
    Task<PaymentCheckoutResult> InitiateCheckoutAsync(Guid organizationId, Guid userId, string packageId, string successUrl, string cancelUrl, CancellationToken ct = default);
    Task<bool> HandleWebhookAsync(string provider, string payload, string signatureHeader, CancellationToken ct = default);
    List<CreditPackageDto> GetAvailablePackages();
}

public interface IPublishingService
{
    Task<List<PublishingAccountDto>> GetConnectedAccountsAsync(Guid organizationId, CancellationToken ct = default);
    Task<PublishingAccountDto> ConnectAccountMockAsync(Guid organizationId, PublishingPlatform platform, string accountName, CancellationToken ct = default);
    Task<PublishingJobDto> SchedulePublishAsync(Guid organizationId, SchedulePublishRequest request, CancellationToken ct = default);
    Task<List<PublishingJobDto>> GetPublishingHistoryAsync(Guid organizationId, CancellationToken ct = default);
}

public interface IAdminService
{
    Task<AdminDashboardMetricsDto> GetDashboardMetricsAsync(CancellationToken ct = default);
    Task<List<WalletDto>> GetAllWalletsAsync(CancellationToken ct = default);
    Task<bool> AdjustCreditsAsync(AdjustCreditRequest request, CancellationToken ct = default);
    Task<List<JobDto>> GetFailedJobsAsync(CancellationToken ct = default);
}

public interface IAnalyticsService
{
    Task<UserAnalyticsDto> GetUserAnalyticsAsync(Guid organizationId, CancellationToken ct = default);
}
