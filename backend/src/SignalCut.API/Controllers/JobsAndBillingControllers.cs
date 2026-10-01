using Microsoft.AspNetCore.Mvc;
using SignalCut.Application.DTOs;
using SignalCut.Application.Interfaces;

namespace SignalCut.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;
    private readonly ICurrentUserContext _userContext;

    public JobsController(IJobService jobService, ICurrentUserContext userContext)
    {
        _jobService = jobService;
        _userContext = userContext;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetJob(Guid id, CancellationToken ct)
    {
        var orgId = _userContext.OrganizationId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
        var job = await _jobService.GetJobStatusAsync(orgId, id, ct);
        if (job == null) return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Job not found" } });

        return Ok(new { success = true, data = job });
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentJobs([FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var orgId = _userContext.OrganizationId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
        var jobs = await _jobService.GetRecentJobsAsync(orgId, limit, ct);
        return Ok(new { success = true, data = jobs });
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelJob(Guid id, CancellationToken ct)
    {
        var orgId = _userContext.OrganizationId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
        var result = await _jobService.CancelJobAsync(orgId, id, ct);
        return Ok(new { success = true, data = result });
    }

    [HttpPost("{id}/retry")]
    public async Task<IActionResult> RetryJob(Guid id, CancellationToken ct)
    {
        var orgId = _userContext.OrganizationId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
        var job = await _jobService.RetryJobAsync(orgId, id, ct);
        return Ok(new { success = true, data = job });
    }
}

[ApiController]
[Route("api/v1/[controller]")]
public class CreditsController : ControllerBase
{
    private readonly ICreditWalletService _creditService;
    private readonly ICurrentUserContext _userContext;

    public CreditsController(ICreditWalletService creditService, ICurrentUserContext userContext)
    {
        _creditService = creditService;
        _userContext = userContext;
    }

    [HttpGet("wallet")]
    public async Task<IActionResult> GetWallet(CancellationToken ct)
    {
        var orgId = _userContext.OrganizationId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
        var wallet = await _creditService.GetWalletAsync(orgId, ct);
        return Ok(new { success = true, data = wallet });
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var orgId = _userContext.OrganizationId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
        var txs = await _creditService.GetTransactionsAsync(orgId, page, pageSize, ct);
        return Ok(new { success = true, data = txs });
    }

    [HttpGet("estimate")]
    public async Task<IActionResult> EstimateCost([FromQuery] string operation, [FromQuery] double durationSeconds = 0, CancellationToken ct = default)
    {
        var orgId = _userContext.OrganizationId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
        var estimation = await _creditService.EstimateCostAsync(orgId, operation, durationSeconds, ct);
        return Ok(new { success = true, data = estimation });
    }
}

[ApiController]
[Route("api/v1/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ICurrentUserContext _userContext;

    public PaymentsController(IPaymentService paymentService, ICurrentUserContext userContext)
    {
        _paymentService = paymentService;
        _userContext = userContext;
    }

    [HttpGet("packages")]
    public IActionResult GetPackages()
    {
        var pkgs = _paymentService.GetAvailablePackages();
        return Ok(new { success = true, data = pkgs });
    }

    public record CreateCheckoutDto(string PackageId, string SuccessUrl, string CancelUrl);

    [HttpPost("checkout")]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutDto request, CancellationToken ct)
    {
        var orgId = _userContext.OrganizationId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
        var userId = _userContext.UserId ?? Guid.Parse("00000000-0000-0000-0000-000000000002");

        var session = await _paymentService.InitiateCheckoutAsync(orgId, userId, request.PackageId, request.SuccessUrl, request.CancelUrl, ct);
        return Ok(new { success = true, data = session });
    }

    // Webhooks: Server-Side signature verification & idempotent ledger crediting
    [HttpPost("webhook/{provider}")]
    public async Task<IActionResult> ProcessWebhook(string provider, CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(ct);

        var signatureHeader = Request.Headers["Stripe-Signature"].FirstOrDefault()
                              ?? Request.Headers["X-Razorpay-Signature"].FirstOrDefault()
                              ?? Request.Headers["signature"].FirstOrDefault()
                              ?? string.Empty;

        var success = await _paymentService.HandleWebhookAsync(provider, payload, signatureHeader, ct);
        if (!success)
        {
            return BadRequest(new { success = false, message = "Webhook signature verification failed or event invalid." });
        }

        return Ok(new { received = true });
    }
}
