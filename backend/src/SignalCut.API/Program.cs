using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
var jwtSecret = builder.Configuration["JWT_SECRET"] ?? "signalcut_production_secret_key_minimum_32_characters_long_123456";
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = false,
        ValidateAudience = false,
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

// CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Seed database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SignalCutDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await db.Database.EnsureCreatedAsync();
    await DataSeeder.SeedAsync(db, hasher);
}

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

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Multi-tenant isolation middleware
app.UseMiddleware<TenantIsolationMiddleware>();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0",
    service = "SignalCut API"
}));

app.Run();

public partial class Program { }
