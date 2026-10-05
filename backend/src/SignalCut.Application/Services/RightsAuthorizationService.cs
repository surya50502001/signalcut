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

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mov", ".mkv", ".webm", ".mp3", ".m4a", ".wav", ".aac"
    };

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4", "video/quicktime", "video/webm", "video/x-matroska",
        "audio/mpeg", "audio/mp4", "audio/wav", "audio/x-wav", "audio/aac", "audio/x-m4a", "audio/mp3",
        "application/octet-stream"
    };

    private const long MaxFileSizeBytes = 500L * 1024 * 1024; // 500 MB max

    private static bool IsValidConfirmationStatement(string? statement)
    {
        if (string.IsNullOrWhiteSpace(statement)) return false;
        var trimmed = statement.Trim();
        return trimmed.Equals("I confirm that I own this content or have permission to use, edit, and publish it.", StringComparison.OrdinalIgnoreCase) ||
               trimmed.Equals("I confirm that I own or have permission to use this content.", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<RightsConfirmationResponse> ConfirmRightsAsync(Guid organizationId, Guid userId, RightsConfirmationRequest request, CancellationToken ct = default)
    {
        var source = await _context.Sources
            .Include(s => s.MediaAssets)
            .FirstOrDefaultAsync(s => s.Id == request.SourceId && s.OrganizationId == organizationId, ct);

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

        var hasUploadedMedia = source.MediaAssets.Any(m => !string.IsNullOrEmpty(m.StorageUrl) || !string.IsNullOrEmpty(m.StorageKey));
        var isLicensedDirect = (request.ClaimedRightsStatus == RightsStatus.LICENSED || request.ClaimedRightsStatus == RightsStatus.PUBLIC_DOMAIN)
                               && !string.IsNullOrEmpty(request.ProofDocumentUrl)
                               && source.AuthorizationStatus == AuthorizationStatus.VERIFIED;

        // A source must have an actual uploaded MediaAsset (file physically stored) or be a
        // verified LICENSED/PUBLIC_DOMAIN source. Provider label alone is NOT sufficient.
        bool canDirectlyAuthorize = hasUploadedMedia || isLicensedDirect;

        source.RightsConfirmedByUserId = userId;
        source.RightsConfirmationTimestamp = DateTime.UtcNow;
        source.RightsConfirmationStatement = request.ConfirmationStatement.Trim();
        source.RightsProofDocumentUrl = request.ProofDocumentUrl;
        source.UpdatedAt = DateTime.UtcNow;

        if (canDirectlyAuthorize)
        {
            source.RightsStatus = request.ClaimedRightsStatus;
            source.AuthorizationStatus = AuthorizationStatus.VERIFIED;
        }
        else
        {
            // Discovered external sources MUST remain DISCOVERY_ONLY until authorized media is uploaded
            source.RightsStatus = RightsStatus.DISCOVERY_ONLY;
            source.AuthorizationStatus = AuthorizationStatus.PENDING;
        }

        // Record immutable audit log
        var audit = new AuditLog
        {
            OrganizationId = organizationId,
            UserId = userId,
            Action = canDirectlyAuthorize ? "RIGHTS_CONFIRMED" : "RIGHTS_CLAIM_RECORDED_PENDING_UPLOAD",
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
                ProofUrl = request.ProofDocumentUrl,
                CanDirectlyAuthorize = canDirectlyAuthorize
            })
        };

        _context.AuditLogs.Add(audit);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Rights claim recorded for source {SourceId} by user {UserId} as {ClaimedStatus} (Effective RightsStatus: {Status}, AuthStatus: {AuthStatus})",
            source.Id, userId, request.ClaimedRightsStatus, source.RightsStatus, source.AuthorizationStatus);

        var message = canDirectlyAuthorize
            ? "Rights confirmed successfully. Source is authorized for clip generation."
            : "This source is available for discovery, but SignalCut does not have an authorized way to retrieve the media. Upload the video/audio you have permission to use to continue.";

        return new RightsConfirmationResponse(
            canDirectlyAuthorize,
            source.Id,
            source.RightsStatus,
            source.AuthorizationStatus,
            source.RightsConfirmationTimestamp.Value,
            message
        );
    }

    public async Task<bool> AssertCanEnterGenerationPipelineAsync(Guid sourceId, CancellationToken ct = default)
    {
        var source = await _context.Sources
            .Include(s => s.MediaAssets)
            .FirstOrDefaultAsync(s => s.Id == sourceId, ct);

        if (source == null)
        {
            throw new NotFoundException(nameof(Source), sourceId);
        }

        var hasUploadedMedia = source.MediaAssets.Any(m => !string.IsNullOrEmpty(m.StorageUrl) || !string.IsNullOrEmpty(m.StorageKey));
        var isVerifiedLicensed = (source.RightsStatus == RightsStatus.LICENSED || source.RightsStatus == RightsStatus.PUBLIC_DOMAIN)
                                 && source.AuthorizationStatus == AuthorizationStatus.VERIFIED;

        // hasAuthorizedMediaAccess requires an ACTUAL uploaded MediaAsset or verified LICENSED/PUBLIC_DOMAIN.
        // Provider label or ContentType alone does NOT grant media access.
        var hasAuthorizedMediaAccess = hasUploadedMedia || isVerifiedLicensed;

        if (!hasAuthorizedMediaAccess || source.RightsStatus == RightsStatus.DISCOVERY_ONLY || source.RightsStatus == RightsStatus.BLOCKED || source.RightsStatus == RightsStatus.UNKNOWN)
        {
            _logger.LogWarning("Access denied to media generation pipeline for Source {SourceId}. External source has no authorized media access. RightsStatus: {Status}, HasMedia: {HasMedia}",
                sourceId, source.RightsStatus, hasUploadedMedia);

            throw new UnauthorizedMediaException(sourceId,
                "This source is available for discovery, but SignalCut does not have an authorized way to retrieve the media. Upload the video/audio you have permission to use to continue.");
        }

        bool isAuthorized = source.RightsStatus switch
        {
            RightsStatus.USER_OWNED => true,
            RightsStatus.USER_AUTHORIZED => source.AuthorizationStatus == AuthorizationStatus.VERIFIED,
            RightsStatus.LICENSED => source.AuthorizationStatus == AuthorizationStatus.VERIFIED,
            RightsStatus.PUBLIC_DOMAIN => true,
            _ => false
        };

        if (!isAuthorized)
        {
            _logger.LogWarning("Access denied to media generation pipeline for Source {SourceId}. RightsStatus: {Status}, AuthStatus: {AuthStatus}",
                sourceId, source.RightsStatus, source.AuthorizationStatus);

            throw new UnauthorizedMediaException(sourceId,
                "This source is available for discovery, but SignalCut does not have an authorized way to retrieve the media. Upload the video/audio you have permission to use to continue.");
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

        // Validate file extension
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
        {
            throw new ValidationException("FileName", $"Unsupported file type '{ext}'. Allowed types: {string.Join(", ", AllowedExtensions)}");
        }

        // Validate MIME type if provided
        if (!string.IsNullOrEmpty(contentType) && !AllowedMimeTypes.Contains(contentType))
        {
            throw new ValidationException("ContentType", $"Unsupported media content type '{contentType}'.");
        }

        // Validate file size limit
        if (fileStream.CanSeek && fileStream.Length > MaxFileSizeBytes)
        {
            throw new ValidationException("FileSize", "File size exceeds the 500 MB maximum limit.");
        }

        // Validate actual container format via magic bytes (independent of client-supplied MIME)
        ValidateMediaContainer(fileStream, ext);

        // Multi-tenant directory isolation: media/{orgId}/{sourceId}/{randomGuid}{ext}
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

    private static void ValidateMediaContainer(Stream stream, string extension)
    {
        if (stream == null || !stream.CanRead)
        {
            throw new ValidationException("FileStream", "Uploaded media stream cannot be read.");
        }

        if (stream.CanSeek)
        {
            if (stream.Length < 12)
            {
                throw new ValidationException("FileContent", "Uploaded file is too small or truncated to be a valid media container.");
            }

            var originalPos = stream.Position;
            try
            {
                stream.Position = 0;
                var header = new byte[16];
                int bytesRead = stream.Read(header, 0, header.Length);
                if (bytesRead < 12)
                {
                    throw new ValidationException("FileContent", "Unable to read media container header.");
                }

                bool isValid = false;
                var ext = extension.ToLowerInvariant();

                switch (ext)
                {
                    case ".mp4":
                    case ".m4a":
                    case ".mov":
                        // ISO Base Media File Format: bytes 4..7 == 'ftyp', 'moov', 'wide', or 'mdat'
                        string boxType = System.Text.Encoding.ASCII.GetString(header, 4, 4);
                        if (boxType == "ftyp" || boxType == "moov" || boxType == "wide" || boxType == "mdat")
                        {
                            isValid = true;
                        }
                        break;

                    case ".mkv":
                    case ".webm":
                        // EBML header: 0x1A, 0x45, 0xDF, 0xA3
                        if (header[0] == 0x1A && header[1] == 0x45 && header[2] == 0xDF && header[3] == 0xA3)
                        {
                            isValid = true;
                        }
                        break;

                    case ".wav":
                        // RIFF header with WAVE format
                        if (header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F' &&
                            header[8] == (byte)'W' && header[9] == (byte)'A' && header[10] == (byte)'V' && header[11] == (byte)'E')
                        {
                            isValid = true;
                        }
                        break;

                    case ".mp3":
                        // ID3v2 tag or MPEG audio frame sync (0xFF, 0xFB/F3/F2/E3/E2)
                        if (header[0] == (byte)'I' && header[1] == (byte)'D' && header[2] == (byte)'3')
                        {
                            isValid = true;
                        }
                        else if (header[0] == 0xFF && (header[1] & 0xE0) == 0xE0)
                        {
                            isValid = true;
                        }
                        break;

                    case ".aac":
                        // ADTS sync word: 0xFF, 0xF1 or 0xF9, or ID3
                        if (header[0] == 0xFF && (header[1] & 0xF0) == 0xF0)
                        {
                            isValid = true;
                        }
                        else if (header[0] == (byte)'I' && header[1] == (byte)'D' && header[2] == (byte)'3')
                        {
                            isValid = true;
                        }
                        break;
                }

                if (!isValid)
                {
                    throw new ValidationException("FileContent", $"File signature does not match expected format for extension '{ext}'. The file container is invalid, corrupted, or unsupported.");
                }
            }
            finally
            {
                stream.Position = originalPos;
            }
        }
    }
}

