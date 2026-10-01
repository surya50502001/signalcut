using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SignalCut.Application.Common;
using SignalCut.Application.Services;
using SignalCut.Domain.Entities;
using SignalCut.Domain.Enums;
using SignalCut.Infrastructure.Persistence;
using SignalCut.Infrastructure.Security;
using Xunit;

namespace SignalCut.Tests;

public class SecurityAndSSRFTests
{
    private SignalCutDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<SignalCutDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new SignalCutDbContext(options);
    }

    [Fact]
    public async Task CriticalTest8_PublishingTokenIsNeverExposedInApiDto()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        const string secretToken = "super_secret_oauth_access_token_xyz987";

        var account = new PublishingAccount
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Platform = PublishingPlatform.YOUTUBE,
            AccountName = "My Channel",
            AccountIdentifier = "@mychannel",
            EncryptedAccessToken = secretToken,
            EncryptedRefreshToken = "refresh_secret_123",
            IsConnected = true
        };

        context.PublishingAccounts.Add(account);
        await context.SaveChangesAsync();

        var publishingService = new PublishingService(context, Array.Empty<Application.Interfaces.IPublishingProvider>(), NullLogger<PublishingService>.Instance);

        // Act
        var accounts = await publishingService.GetConnectedAccountsAsync(orgId);

        // Assert: The returned DTO must NOT contain the access token or refresh token anywhere
        accounts.Should().HaveCount(1);
        var dto = accounts[0];
        dto.AccountName.Should().Be("My Channel");
        dto.Platform.Should().Be(PublishingPlatform.YOUTUBE);

        // Introspect DTO properties to verify secret tokens are completely absent
        var dtoProps = typeof(Application.DTOs.PublishingAccountDto).GetProperties();
        dtoProps.Should().NotContain(p => p.Name.Contains("AccessToken", StringComparison.OrdinalIgnoreCase));
        dtoProps.Should().NotContain(p => p.Name.Contains("RefreshToken", StringComparison.OrdinalIgnoreCase));
        dtoProps.Should().NotContain(p => p.Name.Contains("Secret", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("http://127.0.0.1:8080/admin")]
    [InlineData("http://localhost:5000/secret")]
    [InlineData("http://169.254.169.254/latest/meta-data/")]
    [InlineData("http://10.0.0.1/private-video.mp4")]
    [InlineData("http://192.168.1.1/router")]
    [InlineData("ftp://example.com/video.mp4")]
    [InlineData("file:///etc/passwd")]
    public async Task CriticalTest9_MaliciousUrlsCannotTriggerSSRF(string maliciousUrl)
    {
        // Arrange
        var validator = new MediaSecurityValidator(new HttpClient(), NullLogger<MediaSecurityValidator>.Instance);

        // Act & Assert
        var act = async () => await validator.ValidateUrlSafetyAsync(maliciousUrl);
        await act.Should().ThrowAsync<SecurityException>();
    }
}
