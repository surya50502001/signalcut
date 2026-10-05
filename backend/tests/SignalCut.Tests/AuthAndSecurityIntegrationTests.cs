using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace SignalCut.Tests;

public class AuthAndSecurityIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthAndSecurityIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UnauthenticatedRequest_ToSearch_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/search", new { query = "ai agents" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UnauthenticatedRequest_ToSources_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/sources");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UnauthenticatedRequest_ToClips_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/clips");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public void MissingJwtSecret_InProductionEnvironment_FailsStartup()
    {
        // Arrange: Factory configured for Production environment without JWT_SECRET
        var prodFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("JWT_SECRET", "");
            builder.UseSetting("ALLOWED_ORIGINS", "https://app.signalcut.io");
        });

        // Act & Assert: Starting client must throw InvalidOperationException during host construction
        var act = () => prodFactory.CreateClient();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*JWT_SECRET*required in production*");
    }

    [Fact]
    public void MissingAllowedOrigins_InProductionEnvironment_FailsStartup()
    {
        // Arrange: Factory configured for Production without ALLOWED_ORIGINS
        var prodFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("JWT_SECRET", "cryptographically_secure_random_key_with_at_least_32_characters!!");
            builder.UseSetting("ALLOWED_ORIGINS", "");
        });

        // Act & Assert: Starting client must throw InvalidOperationException
        var act = () => prodFactory.CreateClient();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*ALLOWED_ORIGINS*must be configured in production*");
    }

    [Fact]
    public async Task PublicPackagesEndpoint_AllowsAnonymous()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/payments/packages");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
