using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SignalCut.Application.Common;
using SignalCut.Application.DTOs;
using SignalCut.Application.Interfaces;
using SignalCut.Domain.Entities;
using SignalCut.Domain.Enums;

namespace SignalCut.Infrastructure.Security;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public class BCryptPasswordHasher : IPasswordHasher
{
    public string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password, 11);
    public bool VerifyPassword(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateJwtToken(User user, Organization org);
    string GenerateRefreshToken();
}

public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config)
    {
        _config = config;
    }

    public (string Token, DateTime ExpiresAt) GenerateJwtToken(User user, Organization org)
    {
        var secret = _config["JWT_SECRET"] ?? "signalcut_production_secret_key_minimum_32_characters_long_123456";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddHours(12);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("org_id", org.Id.ToString()),
            new("org_name", org.Name),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("name", user.FullName)
        };

        var token = new JwtSecurityToken(
            issuer: _config["JWT_ISSUER"] ?? "signalcut.app",
            audience: _config["JWT_AUDIENCE"] ?? "signalcut.app",
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}

public interface ITokenEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

public class AesTokenEncryptionService : ITokenEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesTokenEncryptionService(IConfiguration config)
    {
        var secret = config["TOKEN_ENCRYPTION_KEY"] ?? "SignalCutEncryptionSecret32Bytes!!";
        _key = Encoding.UTF8.GetBytes(secret.PadRight(32)[..32]);
        _iv = Encoding.UTF8.GetBytes("SignalCutIV16Byt"[..16]);
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }
        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        var cipherBytes = Convert.FromBase64String(cipherText);
        using var ms = new MemoryStream(cipherBytes);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }
}

public class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var sub = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public Guid? OrganizationId
    {
        get
        {
            var org = User?.FindFirst("org_id")?.Value;
            return Guid.TryParse(org, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value ?? User?.FindFirst(JwtRegisteredClaimNames.Email)?.Value;

    public UserRole? Role
    {
        get
        {
            var roleStr = User?.FindFirst(ClaimTypes.Role)?.Value;
            return Enum.TryParse<UserRole>(roleStr, out var r) ? r : null;
        }
    }
}

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IApplicationDbContext context,
        IPasswordHasher hasher,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _hasher = hasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var emailClean = request.Email.Trim().ToLowerInvariant();
        var exists = await _context.Users.AnyAsync(u => u.Email == emailClean, ct);
        if (exists)
        {
            throw new ConflictException("An account with this email address already exists.");
        }

        var orgName = string.IsNullOrWhiteSpace(request.OrganizationName)
            ? $"{request.FullName.Split(' ')[0]}'s Workspace"
            : request.OrganizationName.Trim();

        var slug = orgName.ToLowerInvariant().Replace(" ", "-").Replace("'", "");
        var org = new Organization
        {
            Name = orgName,
            Slug = $"{slug}-{Guid.NewGuid().ToString("N")[..6]}",
            IsActive = true
        };

        _context.Organizations.Add(org);

        var user = new User
        {
            Organization = org,
            Email = emailClean,
            FullName = request.FullName.Trim(),
            PasswordHash = _hasher.HashPassword(request.Password),
            Role = UserRole.USER,
            IsEmailVerified = true,
            HasUsedFreeTrial = false
        };

        _context.Users.Add(user);

        // Create welcome wallet with 50 credits
        var wallet = new CreditWallet
        {
            Organization = org,
            Balance = 50m,
            ReservedBalance = 0m,
            LifetimeEarned = 50m,
            Currency = "INR"
        };
        _context.CreditWallets.Add(wallet);

        var (token, expiresAt) = _tokenService.GenerateJwtToken(user, org);
        var refreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30);

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Registered new user {Email} in organization {OrgName}", user.Email, org.Name);

        return new AuthResponse(
            token,
            refreshToken,
            expiresAt,
            new UserDto(user.Id, user.Email, user.FullName, user.Role, user.IsEmailVerified, user.HasUsedFreeTrial),
            new OrganizationDto(org.Id, org.Name, org.Slug, wallet.AvailableBalance)
        );
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var emailClean = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .Include(u => u.Organization)
                .ThenInclude(o => o.CreditWallet)
            .FirstOrDefaultAsync(u => u.Email == emailClean, ct);

        if (user == null || !_hasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new ValidationException("Credentials", "Invalid email or password.");
        }

        var (token, expiresAt) = _tokenService.GenerateJwtToken(user, user.Organization);
        var refreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30);

        await _context.SaveChangesAsync(ct);

        var availableCredits = user.Organization.CreditWallet?.AvailableBalance ?? 0m;

        return new AuthResponse(
            token,
            refreshToken,
            expiresAt,
            new UserDto(user.Id, user.Email, user.FullName, user.Role, user.IsEmailVerified, user.HasUsedFreeTrial),
            new OrganizationDto(user.Organization.Id, user.Organization.Name, user.Organization.Slug, availableCredits)
        );
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var user = await _context.Users
            .Include(u => u.Organization)
                .ThenInclude(o => o.CreditWallet)
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, ct);

        if (user == null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
        {
            throw new SecurityException("Refresh token is invalid or expired.");
        }

        var (token, expiresAt) = _tokenService.GenerateJwtToken(user, user.Organization);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30);

        await _context.SaveChangesAsync(ct);

        var availableCredits = user.Organization.CreditWallet?.AvailableBalance ?? 0m;

        return new AuthResponse(
            token,
            newRefreshToken,
            expiresAt,
            new UserDto(user.Id, user.Email, user.FullName, user.Role, user.IsEmailVerified, user.HasUsedFreeTrial),
            new OrganizationDto(user.Organization.Id, user.Organization.Name, user.Organization.Slug, availableCredits)
        );
    }
}
