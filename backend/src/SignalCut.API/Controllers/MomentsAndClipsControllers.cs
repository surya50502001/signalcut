using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalCut.Application.DTOs;
using SignalCut.Application.Interfaces;
using SignalCut.Domain.Enums;

namespace SignalCut.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class MomentsController : ControllerBase
{
    private readonly IMomentService _momentService;
    private readonly ICurrentUserContext _userContext;

    public MomentsController(IMomentService momentService, ICurrentUserContext userContext)
    {
        _momentService = momentService;
        _userContext = userContext;
    }

    public record DetectMomentsRequest(Guid SourceId, MomentObjective Objective = MomentObjective.Educational);

    [HttpPost("detect")]
    public async Task<IActionResult> DetectMoments([FromBody] DetectMomentsRequest request, CancellationToken ct)
    {
        var orgId = _userContext.OrganizationId!.Value;
        var userId = _userContext.UserId!.Value;

        var moments = await _momentService.DetectMomentsAsync(orgId, userId, request.SourceId, request.Objective, ct);
        return Ok(new { success = true, data = moments });
    }

    [HttpGet("source/{sourceId}")]
    public async Task<IActionResult> GetMomentsBySource(Guid sourceId, CancellationToken ct)
    {
        var orgId = _userContext.OrganizationId!.Value;
        var moments = await _momentService.GetMomentsBySourceAsync(orgId, sourceId, ct);
        return Ok(new { success = true, data = moments });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetMomentById(Guid id, CancellationToken ct)
    {
        var orgId = _userContext.OrganizationId!.Value;
        var moment = await _momentService.GetMomentByIdAsync(orgId, id, ct);
        if (moment == null) return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Moment not found" } });

        return Ok(new { success = true, data = moment });
    }

    [HttpPost("{id}/regenerate-copy")]
    public async Task<IActionResult> RegenerateCopy(Guid id, CancellationToken ct)
    {
        var orgId = _userContext.OrganizationId!.Value;
        var updated = await _momentService.RegenerateCopyAsync(orgId, id, ct);
        return Ok(new { success = true, data = updated });
    }
}

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class ClipsController : ControllerBase
{
    private readonly IClipService _clipService;
    private readonly ICurrentUserContext _userContext;

    public ClipsController(IClipService clipService, ICurrentUserContext userContext)
    {
        _clipService = clipService;
        _userContext = userContext;
    }

    [HttpPost]
    public async Task<IActionResult> CreateClip([FromBody] CreateClipRequest request, CancellationToken ct)
    {
        var orgId = _userContext.OrganizationId!.Value;
        var userId = _userContext.UserId!.Value;

        var clip = await _clipService.CreateClipFromMomentAsync(orgId, userId, request, ct);
        return Ok(new { success = true, data = clip });
    }

    [HttpGet]
    public async Task<IActionResult> GetClips([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var orgId = _userContext.OrganizationId!.Value;
        var clips = await _clipService.GetClipsAsync(orgId, page, pageSize, ct);
        return Ok(new { success = true, data = clips });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetClipById(Guid id, CancellationToken ct)
    {
        var orgId = _userContext.OrganizationId!.Value;
        var clip = await _clipService.GetClipByIdAsync(orgId, id, ct);
        if (clip == null) return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Clip not found" } });

        return Ok(new { success = true, data = clip });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClip(Guid id, [FromBody] UpdateClipRequest request, CancellationToken ct)
    {
        var orgId = _userContext.OrganizationId!.Value;
        var clip = await _clipService.UpdateClipAsync(orgId, id, request, ct);
        return Ok(new { success = true, data = clip });
    }

    [HttpPost("{id}/render")]
    public async Task<IActionResult> RenderClip(Guid id, CancellationToken ct)
    {
        var orgId = _userContext.OrganizationId!.Value;
        var userId = _userContext.UserId!.Value;

        var job = await _clipService.QueueRenderAsync(orgId, userId, id, ct);
        return Ok(new { success = true, data = job });
    }
}
