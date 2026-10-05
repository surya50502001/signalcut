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
    private readonly IObjectStorage? _storage;

    public RightsAuthorizationService(
        IApplicationDbContext context,
        ILogger<RightsAuthorizationService> logger,
        IObjectStorage? storage = null)
    {
        _context = context;
        _logger = logger;
        _storage = storage;
    }

    private static bool IsValidConfirmationStatement(string? statement)
    {
        if (string.IsNullOrWhiteSpace(statement)) return false;
        var trimmed = statement.Trim();
        return trimmed.Equals("I confirm that I own this content or have permission to use, edit, and publish it.", StringComparison.OrdinalIgnoreCase) ||
               trimmed.Equals("I confirm that I own or have permission to use this content.", StringComparison.OrdinalIgnoreCase);
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
        if (!IsValidConfirmationStatement(request.ConfirmationStatement))
        {
            throw new ValidationException("ConfirmationStatement", "Explicit statement 'I confirm that I own this content or have permission to use, edit, and publish it.' or 'I confirm that I own or have permission to use this content.' is required.");
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

    public async Task<UploadMediaResultDto> UploadAndAuthorizeMediaAsync(
        Guid organizationId,
        Guid userId,
        Guid sourceId,
        Stream fileStream,
        string fileName,
        string contentType,
        RightsConfirmationRequest confirmation,
        CancellationToken ct = default)
    {
        var source = await _context.Sources.FirstOrDefaultAsync(s => s.Id == sourceId && s.OrganizationId == organizationId, ct);
        if (source == null)
        {
            throw new NotFoundException(nameof(Source), sourceId);
        }

        if (_storage == null)
        {
            throw new InvalidOperationException("Storage provider is not configured for media uploads.");
        }

        // Validate claimed rights status is an authorized tier
        if (confirmation.ClaimedRightsStatus == RightsStatus.DISCOVERY_ONLY ||
            confirmation.ClaimedRightsStatus == RightsStatus.BLOCKED ||
            confirmation.ClaimedRightsStatus == RightsStatus.UNKNOWN)
        {
            throw new ValidationException("ClaimedRightsStatus", "You can only confirm authorization for USER_OWNED, USER_AUTHORIZED, LICENSED, or PUBLIC_DOMAIN content.");
        }

        // Validate affirmative confirmation statement
        if (!IsValidConfirmationStatement(confirmation.ConfirmationStatement))
        {
            throw new ValidationException("ConfirmationStatement", "Explicit statement 'I confirm that I own this content or have permission to use, edit, and publish it.' or 'I confirm that I own or have permission to use this content.' is required.");
        }

        // Store file
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var key = $"media/{organizationId:N}/{source.Id:N}/{Guid.NewGuid():N}{ext}";
        var storageUrl = await _storage.UploadAsync(key, fileStream, contentType, ct);

        long fileSize = 0;
        try
        {
            if (fileStream.CanSeek) fileSize = fileStream.Length;
        }
        catch { /* ignore */ }

        // Create MediaAsset entity
        var mediaAsset = new MediaAsset
        {
            OrganizationId = organizationId,
            SourceId = source.Id,
            AssetType = contentType.StartsWith("audio", StringComparison.OrdinalIgnoreCase) ? "SOURCE_AUDIO" : "SOURCE_VIDEO",
            StorageKey = key,
            StorageUrl = storageUrl,
            ContentType = contentType,
            FileSizeBytes = fileSize
        };
        _context.MediaAssets.Add(mediaAsset);

        // Transition source from DISCOVERY_ONLY to authorized state
        source.RightsStatus = confirmation.ClaimedRightsStatus;
        source.AuthorizationStatus = AuthorizationStatus.VERIFIED;
        source.RightsConfirmedByUserId = userId;
        source.RightsConfirmationTimestamp = DateTime.UtcNow;
        source.RightsConfirmationStatement = confirmation.ConfirmationStatement.Trim();
        source.RightsProofDocumentUrl = confirmation.ProofDocumentUrl;
        source.Url = storageUrl; // point source to authorized media asset
        source.UpdatedAt = DateTime.UtcNow;

        // Record immutable audit log
        var audit = new AuditLog
        {
            OrganizationId = organizationId,
            UserId = userId,
            Action = "MEDIA_UPLOADED_AND_RIGHTS_CONFIRMED",
            ResourceType = "Source",
            ResourceId = source.Id.ToString(),
            DetailsJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                SourceId = source.Id,
                Title = source.Title,
                FileName = fileName,
                StorageKey = key,
                StorageUrl = storageUrl,
                ClaimedStatus = confirmation.ClaimedRightsStatus.ToString(),
                ConfirmedBy = userId,
                Timestamp = source.RightsConfirmationTimestamp,
                ProofUrl = confirmation.ProofDocumentUrl
            })
        };
        _context.AuditLogs.Add(audit);

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Media uploaded and rights confirmed for source {SourceId} by user {UserId} as {Status}",
            source.Id, userId, source.RightsStatus);

        return new UploadMediaResultDto(
            Success: true,
            SourceId: source.Id,
            MediaAssetId: mediaAsset.Id,
            StorageUrl: storageUrl,
            RightsStatus: source.RightsStatus,
            AuthorizationStatus: source.AuthorizationStatus,
            ConfirmedAt: source.RightsConfirmationTimestamp.Value,
            Message: "Media uploaded successfully and source authorized for clip generation."
        );
    }
}

