using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SignalCut.Application.Common;
using SignalCut.Application.DTOs;
using SignalCut.Application.Interfaces;
using SignalCut.Domain.Enums;

namespace SignalCut.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchDiscoveryService _searchService;
    private readonly ICurrentUserContext _userContext;

    public SearchController(ISearchDiscoveryService searchService, ICurrentUserContext userContext)
    {
        _searchService = searchService;
        _userContext = userContext;
    }

    [HttpPost]
    public async Task<IActionResult> SearchTopic([FromBody] SearchQueryRequest request, CancellationToken ct)
    {
        // Allow unauthenticated demo searches using a demo organization or authenticated organization
        var orgId = _userContext.OrganizationId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
        var userId = _userContext.UserId ?? Guid.Parse("00000000-0000-0000-0000-000000000002");

        var results = await _searchService.SearchTopicAsync(orgId, userId, request, ct);
        return Ok(new { success = true, data = results });
    }
}

[ApiController]
[Route("api/v1/[controller]")]
public class SourcesController : ControllerBase
{
    private readonly ISearchDiscoveryService _searchService;
    private readonly IRightsAuthorizationService _rightsService;
    private readonly ICurrentUserContext _userContext;

    public SourcesController(
        ISearchDiscoveryService searchService,
        IRightsAuthorizationService rightsService,
        ICurrentUserContext userContext)
    {
        _searchService = searchService;
        _rightsService = rightsService;
        _userContext = userContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetSources([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var orgId = _userContext.OrganizationId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
        var sources = await _searchService.GetDiscoveredSourcesAsync(orgId, page, pageSize, ct);
        return Ok(new { success = true, data = sources });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSourceById(Guid id, CancellationToken ct)
    {
        var orgId = _userContext.OrganizationId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
        var source = await _searchService.GetSourceByIdAsync(orgId, id, ct);
        if (source == null) return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Source not found" } });

        return Ok(new { success = true, data = source });
    }

    // CRITICAL: Explicit Rights Confirmation Gate
    [HttpPost("confirm-rights")]
    public async Task<IActionResult> ConfirmRights([FromBody] RightsConfirmationRequest request, CancellationToken ct)
    {
        var orgId = _userContext.OrganizationId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
        var userId = _userContext.UserId ?? Guid.Parse("00000000-0000-0000-0000-000000000002");

        var response = await _rightsService.ConfirmRightsAsync(orgId, userId, request, ct);
        return Ok(new { success = true, data = response });
    }

    private static readonly HashSet<string> AllowedUploadExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mov", ".mkv", ".webm", ".mp3", ".m4a", ".wav", ".aac"
    };

    private static readonly HashSet<string> AllowedUploadMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4", "video/quicktime", "video/webm", "video/x-matroska",
        "audio/mpeg", "audio/mp4", "audio/wav", "audio/x-wav", "audio/aac", "audio/x-m4a", "audio/mp3",
        "application/octet-stream"
    };

    private const long MaxUploadSizeBytes = 500L * 1024 * 1024; // 500 MB max

    [HttpPost("{id}/upload-media")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadMedia(
        Guid id,
        [FromForm] IFormFile file,
        [FromForm] string confirmationStatement,
        [FromForm] RightsStatus claimedRightsStatus = RightsStatus.USER_AUTHORIZED,
        [FromForm] string? proofDocumentUrl = null,
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { success = false, error = new { code = "INVALID_FILE", message = "A video or audio file must be uploaded." } });
        }

        if (file.Length > MaxUploadSizeBytes)
        {
            return BadRequest(new { success = false, error = new { code = "FILE_TOO_LARGE", message = "Uploaded file exceeds the maximum allowed size of 500 MB." } });
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !AllowedUploadExtensions.Contains(ext))
        {
            return BadRequest(new { success = false, error = new { code = "INVALID_EXTENSION", message = $"File extension '{ext}' is not supported. Allowed formats: {string.Join(", ", AllowedUploadExtensions)}" } });
        }

        if (!string.IsNullOrEmpty(file.ContentType) && !AllowedUploadMimeTypes.Contains(file.ContentType))
        {
            return BadRequest(new { success = false, error = new { code = "INVALID_MIME_TYPE", message = $"MIME type '{file.ContentType}' is not permitted." } });
        }

        var sanitizedFileName = Path.GetFileName(file.FileName);

        var orgId = _userContext.OrganizationId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
        var userId = _userContext.UserId ?? Guid.Parse("00000000-0000-0000-0000-000000000002");

        using var stream = file.OpenReadStream();
        var confirmReq = new RightsConfirmationRequest(id, claimedRightsStatus, confirmationStatement, proofDocumentUrl);
        var response = await _rightsService.UploadAndAuthorizeMediaAsync(
            orgId,
            userId,
            id,
            stream,
            sanitizedFileName,
            file.ContentType,
            confirmReq,
            ct);

        return Ok(new { success = true, data = response });
    }
}
