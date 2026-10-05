using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalCut.Application.DTOs;
using SignalCut.Application.Interfaces;
using SignalCut.Domain.Enums;

namespace SignalCut.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class PublishingController : ControllerBase
{
    private readonly IPublishingService _publishingService;
    private readonly ICurrentUserContext _userContext;

    public PublishingController(IPublishingService publishingService, ICurrentUserContext userContext)
    {
        _publishingService = publishingService;
        _userContext = userContext;
    }

    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts(CancellationToken ct)
    {
        var orgId = _userContext.OrganizationId!.Value;
        var accounts = await _publishingService.GetConnectedAccountsAsync(orgId, ct);
        return Ok(new { success = true, data = accounts });
    }

    public record ConnectAccountRequest(PublishingPlatform Platform, string AccountName);

    [HttpPost("connect-account")]
    public async Task<IActionResult> ConnectAccount([FromBody] ConnectAccountRequest request, CancellationToken ct)
    {
        var orgId = _userContext.OrganizationId!.Value;
        var account = await _publishingService.ConnectAccountMockAsync(orgId, request.Platform, request.AccountName, ct);
        return Ok(new { success = true, data = account });
    }

    [HttpPost("schedule")]
    public async Task<IActionResult> SchedulePublish([FromBody] SchedulePublishRequest request, CancellationToken ct)
    {
        var orgId = _userContext.OrganizationId!.Value;
        var job = await _publishingService.SchedulePublishAsync(orgId, request, ct);
        return Ok(new { success = true, data = job });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken ct)
    {
        var orgId = _userContext.OrganizationId!.Value;
        var history = await _publishingService.GetPublishingHistoryAsync(orgId, ct);
        return Ok(new { success = true, data = history });
    }
}

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ICurrentUserContext _userContext;

    public AnalyticsController(IAnalyticsService analyticsService, ICurrentUserContext userContext)
    {
        _analyticsService = analyticsService;
        _userContext = userContext;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken ct)
    {
        var orgId = _userContext.OrganizationId!.Value;
        var analytics = await _analyticsService.GetUserAnalyticsAsync(orgId, ct);
        return Ok(new { success = true, data = analytics });
    }
}

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics(CancellationToken ct)
    {
        var metrics = await _adminService.GetDashboardMetricsAsync(ct);
        return Ok(new { success = true, data = metrics });
    }

    [HttpGet("wallets")]
    public async Task<IActionResult> GetWallets(CancellationToken ct)
    {
        var wallets = await _adminService.GetAllWalletsAsync(ct);
        return Ok(new { success = true, data = wallets });
    }

    [HttpPost("adjust-credits")]
    public async Task<IActionResult> AdjustCredits([FromBody] AdjustCreditRequest request, CancellationToken ct)
    {
        var result = await _adminService.AdjustCreditsAsync(request, ct);
        return Ok(new { success = true, data = result });
    }

    [HttpGet("failed-jobs")]
    public async Task<IActionResult> GetFailedJobs(CancellationToken ct)
    {
        var failed = await _adminService.GetFailedJobsAsync(ct);
        return Ok(new { success = true, data = failed });
    }
}
