using SignalCut.Application.Common;
using SignalCut.Application.Interfaces;

namespace SignalCut.API.Middleware;

public class TenantIsolationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantIsolationMiddleware> _logger;

    public TenantIsolationMiddleware(RequestDelegate next, ILogger<TenantIsolationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentUserContext userContext)
    {
        // Enforce that authenticated requests have a valid OrganizationId claim
        if (userContext.IsAuthenticated && (!userContext.OrganizationId.HasValue || userContext.OrganizationId.Value == Guid.Empty))
        {
            _logger.LogWarning("Authenticated user {UserId} lacks a valid tenant organization binding.", userContext.UserId);
            throw new TenantAccessDeniedException("User is not associated with an active organization.");
        }

        await _next(context);
    }
}
