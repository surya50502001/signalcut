using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalCut.Application.DTOs;
using SignalCut.Application.Interfaces;
using SignalCut.Infrastructure.Security;

namespace SignalCut.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserContext _userContext;

    public AuthController(IAuthService authService, ICurrentUserContext userContext)
    {
        _authService = authService;
        _userContext = userContext;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var response = await _authService.RegisterAsync(request, ct);
        return Ok(new { success = true, data = response });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var response = await _authService.LoginAsync(request, ct);
        return Ok(new { success = true, data = response });
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var response = await _authService.RefreshTokenAsync(request, ct);
        return Ok(new { success = true, data = response });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        return Ok(new
        {
            success = true,
            data = new
            {
                userId = _userContext.UserId,
                organizationId = _userContext.OrganizationId,
                email = _userContext.Email,
                role = _userContext.Role?.ToString()
            }
        });
    }
}
