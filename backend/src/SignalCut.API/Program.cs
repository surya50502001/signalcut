using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using SignalCut.API.Middleware;
using SignalCut.Infrastructure;
using SignalCut.Infrastructure.Persistence;
using SignalCut.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// ─── JWT Secret Guard ─────────────────────────────────────────────────────────
// In production the secret MUST be injected via environment variable.
// Startup is aborted if the secret is absent or too short.
var jwtSecret = builder.Configuration["JWT_SECRET"];
var isDevelopment = builder.Environment.IsDevelopment();

if (string.IsNullOrWhiteSpace(jwtSecret))
{
    if (isDevelopment)
    {
        // Convenient fallback for local dev only
        jwtSecret = "signalcut_dev_only_secret_key_minimum_32_characters_dev_123456";
        Log.Warning("JWT_SECRET not configured — using insecure development default. This MUST be set in production.");
    }
    else
    {
        throw new InvalidOperationException(
            "JWT_SECRET environment variable is required in production. " +
            "Set a cryptographically random value of at least 32 characters.");
    }
}

if (jwtSecret.Length < 32)
{
    if (!isDevelopment)
        throw new InvalidOperationException(
            $"JWT_SECRET is too short ({jwtSecret.Length} chars). Minimum 32 characters required in production.");
    Log.Warning("JWT_SECRET is shorter than 32 characters — acceptable only in development.");
}
// ─────────────────────────────────────────────────────────────────────────────

// Add services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

// Infrastructure Layer Injection
builder.Services.AddInfrastructureServices(builder.Configuration);

// Authentication & JWT Bearer
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Require HTTPS in production; allow HTTP in development only
    options.RequireHttpsMetadata = !isDevelopment;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JWT_ISSUER"] ?? "signalcut.app",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT_AUDIENCE"] ?? "signalcut.app",
        ClockSkew = TimeSpan.Zero
    };
});

// Swagger / OpenAPI with Bearer Authorization
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SignalCut API",
        Version = "v1",
        Description = "AI-Powered Content Discovery & Repurposing SaaS Platform"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ─── CORS Policy ─────────────────────────────────────────────────────────────
// Origins are loaded from ALLOWED_ORIGINS config (comma-separated list).
// In Development, sensible localhost defaults are used.
// AllowAnyOrigin() is NOT used — it is incompatible with AllowCredentials()
// and exposes the API to any domain.
var rawOrigins = builder.Configuration["ALLOWED_ORIGINS"] ?? string.Empty;
var allowedOrigins = rawOrigins
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Where(o => !string.IsNullOrWhiteSpace(o))
    .ToArray();

if (allowedOrigins.Length == 0)
{
    if (isDevelopment)
    {
        allowedOrigins = new[] { "http://localhost:5173", "http://localhost:3000", "http://localhost:5174" };
        Log.Warning("ALLOWED_ORIGINS not configured — using localhost defaults for development.");
    }
    else
    {
        // In production, refuse to start with no allowed origins configured
        throw new InvalidOperationException(
            "ALLOWED_ORIGINS environment variable must be configured in production. " +
            "Set it to a comma-separated list of permitted frontend origins (e.g. https://app.signalcut.io).");
    }
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalCutCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
// ─────────────────────────────────────────────────────────────────────────────

var app = builder.Build();

// ─── Database Startup ─────────────────────────────────────────────────────────
// Development / Test: EnsureCreated for quick iteration.
// Production: MigrateAsync so migration history is respected and schema is never wiped.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SignalCutDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Test"))
        await db.Database.EnsureCreatedAsync();
    else
        await db.Database.MigrateAsync();

    await DataSeeder.SeedAsync(db, hasher);
}
// ─────────────────────────────────────────────────────────────────────────────

// Global Exception Handler Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Static Files (for renders and storage media)
app.UseStaticFiles();

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SignalCut API v1");
    c.RoutePrefix = "swagger";
});

// CORS must be before Auth middleware
app.UseCors("SignalCutCors");

app.UseAuthentication();
app.UseAuthorization();

// Multi-tenant isolation middleware
app.UseMiddleware<TenantIsolationMiddleware>();

app.MapControllers();

// Health check endpoint (unauthenticated — for load balancer probes)
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0",
    service = "SignalCut API"
}));

app.Run();

public partial class Program { }


