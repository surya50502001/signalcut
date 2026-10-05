using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SignalCut.Application.Common;
using SignalCut.Application.DTOs;
using SignalCut.Application.Interfaces;
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

    [Fact]
    public async Task UploadAndAuthorizeMedia_WithValidStatementAndFile_CreatesMediaAssetAndAuthorizesSource()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var storage = new TestObjectStorage();
        var rightsService = new RightsAuthorizationService(context, NullLogger<RightsAuthorizationService>.Instance, storage);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var source = new Source
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Title = "Discovery Podcast Episode",
            Url = "https://youtube.com/watch?v=podcast123",
            RightsStatus = RightsStatus.DISCOVERY_ONLY,
            AuthorizationStatus = AuthorizationStatus.NOT_REQUIRED
        };

        context.Sources.Add(source);
        await context.SaveChangesAsync();

        // Initially, generation pipeline assertion MUST fail
        var initialAssert = async () => await rightsService.AssertCanEnterGenerationPipelineAsync(source.Id);
        await initialAssert.Should().ThrowAsync<UnauthorizedMediaException>();

        // Act: User uploads authorized media and confirms statement
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("fake video binary data"));
        var confirmReq = new RightsConfirmationRequest(
            source.Id,
            RightsStatus.USER_AUTHORIZED,
            "I confirm that I own this content or have permission to use, edit, and publish it."
        );

        var result = await rightsService.UploadAndAuthorizeMediaAsync(
            orgId,
            userId,
            source.Id,
            stream,
            "episode.mp4",
            "video/mp4",
            confirmReq
        );

        // Assert
        result.Success.Should().BeTrue();
        result.RightsStatus.Should().Be(RightsStatus.USER_AUTHORIZED);
        result.AuthorizationStatus.Should().Be(AuthorizationStatus.VERIFIED);
        result.StorageUrl.Should().Contain("/storage/media/");

        // Source entity must be updated to authorized
        var updatedSource = await context.Sources.FirstOrDefaultAsync(s => s.Id == source.Id);
        updatedSource.Should().NotBeNull();
        updatedSource!.RightsStatus.Should().Be(RightsStatus.USER_AUTHORIZED);
        updatedSource.AuthorizationStatus.Should().Be(AuthorizationStatus.VERIFIED);
        updatedSource.RightsConfirmedByUserId.Should().Be(userId);

        // MediaAsset must be created and linked to source
        var mediaAsset = await context.MediaAssets.FirstOrDefaultAsync(m => m.SourceId == source.Id);
        mediaAsset.Should().NotBeNull();
        mediaAsset!.OrganizationId.Should().Be(orgId);
        mediaAsset.ContentType.Should().Be("video/mp4");
        mediaAsset.AssetType.Should().Be("SOURCE_VIDEO");

        // Audit log must be recorded
        var audit = await context.AuditLogs.FirstOrDefaultAsync(a => a.Action == "MEDIA_UPLOADED_AND_RIGHTS_CONFIRMED" && a.ResourceId == source.Id.ToString());
        audit.Should().NotBeNull();

        // Generation pipeline assertion must now pass!
        var pipelinePass = await rightsService.AssertCanEnterGenerationPipelineAsync(source.Id);
        pipelinePass.Should().BeTrue();
    }

    [Fact]
    public async Task UploadAndAuthorizeMedia_WrongTenant_ThrowsNotFoundException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var storage = new TestObjectStorage();
        var rightsService = new RightsAuthorizationService(context, NullLogger<RightsAuthorizationService>.Instance, storage);

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var source = new Source
        {
            Id = Guid.NewGuid(),
            OrganizationId = tenantA,
            Title = "Tenant A Media",
            RightsStatus = RightsStatus.DISCOVERY_ONLY
        };
        context.Sources.Add(source);
        await context.SaveChangesAsync();

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("fake video binary"));
        var confirmReq = new RightsConfirmationRequest(
            source.Id,
            RightsStatus.USER_OWNED,
            "I confirm that I own this content or have permission to use, edit, and publish it."
        );

        // Act & Assert: Tenant B trying to upload to Tenant A's source must throw NotFoundException
        var act = async () => await rightsService.UploadAndAuthorizeMediaAsync(
            tenantB,
            Guid.NewGuid(),
            source.Id,
            stream,
            "video.mp4",
            "video/mp4",
            confirmReq
        );

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private class TestObjectStorage : IObjectStorage
    {
        public Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default)
            => Task.FromResult($"/storage/{key}");
        public Task<Stream> DownloadAsync(string key, CancellationToken ct = default)
            => Task.FromResult<Stream>(new MemoryStream());
        public Task<string> GetSignedUrlAsync(string key, TimeSpan expiresIn)
            => Task.FromResult($"/storage/{key}?sig=test");
        public Task<bool> DeleteAsync(string key, CancellationToken ct = default)
            => Task.FromResult(true);
    }
}

