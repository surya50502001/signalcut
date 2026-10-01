using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SignalCut.Application.Common;
using SignalCut.Application.DTOs;
using SignalCut.Application.Services;
using SignalCut.Domain.Entities;
using SignalCut.Domain.Enums;
using SignalCut.Infrastructure.Persistence;
using Xunit;

namespace SignalCut.Tests;

public class RightsAndMediaAuthorizationTests
{
    private SignalCutDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<SignalCutDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new SignalCutDbContext(options);
    }

    [Fact]
    public async Task CriticalTest6_UnauthorizedMediaCannotEnterGenerationPipeline()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var rightsService = new RightsAuthorizationService(context, NullLogger<RightsAuthorizationService>.Instance);

        var orgId = Guid.NewGuid();
        var discoverySource = new Source
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Title = "Public Web Conference",
            Url = "https://youtube.com/watch?v=sample",
            RightsStatus = RightsStatus.DISCOVERY_ONLY, // Not authorized for generation!
            AuthorizationStatus = AuthorizationStatus.NOT_REQUIRED
        };

        context.Sources.Add(discoverySource);
        await context.SaveChangesAsync();

        // Act & Assert: Attempting to assert pipeline access for DISCOVERY_ONLY media must throw UnauthorizedMediaException
        var act = async () => await rightsService.AssertCanEnterGenerationPipelineAsync(discoverySource.Id);

        var ex = await act.Should().ThrowAsync<UnauthorizedMediaException>();
        ex.Which.SourceId.Should().Be(discoverySource.Id);
        ex.Which.Code.Should().Be("UNAUTHORIZED_MEDIA");
    }

    [Fact]
    public async Task RightsConfirmation_WithExplicitStatement_GrantsAccess()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var rightsService = new RightsAuthorizationService(context, NullLogger<RightsAuthorizationService>.Instance);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var source = new Source
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Title = "User's Own Podcast",
            Url = "https://example.com/podcast",
            RightsStatus = RightsStatus.DISCOVERY_ONLY,
            AuthorizationStatus = AuthorizationStatus.NOT_REQUIRED
        };

        context.Sources.Add(source);
        await context.SaveChangesAsync();

        // Act: User provides affirmative confirmation
        var confirmReq = new RightsConfirmationRequest(
            source.Id,
            RightsStatus.USER_OWNED,
            "I confirm that I own or have permission to use this content.",
            "https://documents.example.com/license.pdf"
        );

        var response = await rightsService.ConfirmRightsAsync(orgId, userId, confirmReq);

        // Assert
        response.Success.Should().BeTrue();
        response.RightsStatus.Should().Be(RightsStatus.USER_OWNED);
        response.AuthorizationStatus.Should().Be(AuthorizationStatus.VERIFIED);

        // Now pipeline assertion must pass
        var canEnter = await rightsService.AssertCanEnterGenerationPipelineAsync(source.Id);
        canEnter.Should().BeTrue();

        // Audit log must be recorded
        var audit = await context.AuditLogs.FirstOrDefaultAsync(a => a.ResourceId == source.Id.ToString());
        audit.Should().NotBeNull();
        audit!.Action.Should().Be("RIGHTS_CONFIRMED");
    }

    [Fact]
    public async Task RightsConfirmation_WithInvalidStatement_FailsValidation()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var rightsService = new RightsAuthorizationService(context, NullLogger<RightsAuthorizationService>.Instance);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var source = new Source
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Title = "Random video",
            Url = "https://example.com/video",
            RightsStatus = RightsStatus.DISCOVERY_ONLY
        };

        context.Sources.Add(source);
        await context.SaveChangesAsync();

        // Act: User provides invalid/vague statement
        var confirmReq = new RightsConfirmationRequest(
            source.Id,
            RightsStatus.USER_OWNED,
            "I think this is fair use",
            null
        );

        var act = async () => await rightsService.ConfirmRightsAsync(orgId, userId, confirmReq);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*'I confirm that I own or have permission to use this content.' is required.*");
    }
}
