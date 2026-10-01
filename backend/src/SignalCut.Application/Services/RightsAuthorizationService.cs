using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SignalCut.Application.Common;
using SignalCut.Application.DTOs;
using SignalCut.Application.Interfaces;
using SignalCut.Domain.Entities;
using SignalCut.Domain.Enums;

namespace SignalCut.Application.Services;

public class RightsAuthorizationService : IRightsAuthorizationService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<RightsAuthorizationService> _logger;

    public RightsAuthorizationService(IApplicationDbContext context, ILogger<RightsAuthorizationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RightsConfirmationResponse> ConfirmRightsAsync(Guid organizationId, Guid userId, RightsConfirmationRequest request, CancellationToken ct = default)
    {
        var source = await _context.Sources.FirstOrDefaultAsync(s => s.Id == request.SourceId && s.OrganizationId == organizationId, ct);
        if (source == null)
        {
            throw new NotFoundException(nameof(Source), request.SourceId);
        }

        // Validate that claimed rights status is an authorized tier
        if (request.ClaimedRightsStatus == RightsStatus.DISCOVERY_ONLY ||
            request.ClaimedRightsStatus == RightsStatus.BLOCKED ||
            request.ClaimedRightsStatus == RightsStatus.UNKNOWN)
        {
            throw new ValidationException("ClaimedRightsStatus", "You can only confirm authorization for USER_OWNED, USER_AUTHORIZED, LICENSED, or PUBLIC_DOMAIN content.");
        }

        // Validate affirmative confirmation statement
        if (string.IsNullOrWhiteSpace(request.ConfirmationStatement) ||
            !request.ConfirmationStatement.Trim().Equals("I confirm that I own or have permission to use this content.", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("ConfirmationStatement", "Explicit statement 'I confirm that I own or have permission to use this content.' is required.");
        }

        source.RightsStatus = request.ClaimedRightsStatus;
        source.AuthorizationStatus = AuthorizationStatus.VERIFIED;
        source.RightsConfirmedByUserId = userId;
        source.RightsConfirmationTimestamp = DateTime.UtcNow;
        source.RightsConfirmationStatement = request.ConfirmationStatement.Trim();
        source.RightsProofDocumentUrl = request.ProofDocumentUrl;
        source.UpdatedAt = DateTime.UtcNow;

        // Record immutable audit log
        var audit = new AuditLog
        {
            OrganizationId = organizationId,
            UserId = userId,
            Action = "RIGHTS_CONFIRMED",
            ResourceType = "Source",
            ResourceId = source.Id.ToString(),
            DetailsJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                SourceId = source.Id,
                Title = source.Title,
                Url = source.Url,
                ClaimedStatus = request.ClaimedRightsStatus.ToString(),
                ConfirmedBy = userId,
                Timestamp = source.RightsConfirmationTimestamp,
                ProofUrl = request.ProofDocumentUrl
            })
        };

        _context.AuditLogs.Add(audit);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Rights confirmed for source {SourceId} by user {UserId} as {Status}", source.Id, userId, source.RightsStatus);

        return new RightsConfirmationResponse(
            true,
            source.Id,
            source.RightsStatus,
            source.AuthorizationStatus,
            source.RightsConfirmationTimestamp.Value,
            "Rights confirmed successfully. Source is authorized for clip generation."
        );
    }

    public async Task<bool> AssertCanEnterGenerationPipelineAsync(Guid sourceId, CancellationToken ct = default)
    {
        var source = await _context.Sources.FirstOrDefaultAsync(s => s.Id == sourceId, ct);
        if (source == null)
        {
            throw new NotFoundException(nameof(Source), sourceId);
        }

        // Section #4: Only USER_OWNED, USER_AUTHORIZED, LICENSED, or PUBLIC_DOMAIN content can enter the media generation pipeline.
        bool isAuthorized = source.RightsStatus switch
        {
            RightsStatus.USER_OWNED => true,
            RightsStatus.USER_AUTHORIZED => source.AuthorizationStatus == AuthorizationStatus.VERIFIED,
            RightsStatus.LICENSED => true,
            RightsStatus.PUBLIC_DOMAIN => true,
            RightsStatus.DISCOVERY_ONLY => false,
            RightsStatus.BLOCKED => false,
            RightsStatus.UNKNOWN => false,
            _ => false
        };

        if (!isAuthorized)
        {
            _logger.LogWarning("Access denied to media generation pipeline for Source {SourceId}. RightsStatus: {Status}, AuthStatus: {AuthStatus}",
                sourceId, source.RightsStatus, source.AuthorizationStatus);

            throw new UnauthorizedMediaException(sourceId,
                $"Content with rights status '{source.RightsStatus}' cannot enter the media generation pipeline. Explicit rights confirmation is required.");
        }

        return true;
    }
}
