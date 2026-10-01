using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalCut.Application.Common;
using SignalCut.Application.DTOs;
using SignalCut.Application.Interfaces;

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
}
