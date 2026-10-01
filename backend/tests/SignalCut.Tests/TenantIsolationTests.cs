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

public class TenantIsolationTests
{
    private SignalCutDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<SignalCutDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new SignalCutDbContext(options);
    }

    [Fact]
    public async Task CriticalTest5_UserCannotAccessAnotherOrganizationData()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var orgA = new Organization { Id = Guid.NewGuid(), Name = "Org A", Slug = "org-a" };
        var orgB = new Organization { Id = Guid.NewGuid(), Name = "Org B", Slug = "org-b" };

        var sourceA = new Source
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgA.Id,
            Title = "Confidential Org A Internal Demo",
            Url = "https://internal.orga.com/demo.mp4",
            RightsStatus = RightsStatus.USER_OWNED
        };

        var clipA = new Clip
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgA.Id,
            MomentId = Guid.NewGuid(),
            Title = "Org A Secret Project Clip"
        };

        context.Organizations.AddRange(orgA, orgB);
        context.Sources.Add(sourceA);
        context.Clips.Add(clipA);
        await context.SaveChangesAsync();

        var clipService = new ClipService(context, null!, null!, null!, NullLogger<ClipService>.Instance);
        var searchService = new SearchDiscoveryService(context, null!, NullLogger<SearchDiscoveryService>.Instance);

        // Act & Assert 1: Org B queries Clip belonging to Org A -> must return null
        var clipFromOrgB = await clipService.GetClipByIdAsync(orgB.Id, clipA.Id);
        clipFromOrgB.Should().BeNull();

        // Act & Assert 2: Org B attempts to update Clip belonging to Org A -> throws NotFoundException
        var updateReq = new UpdateClipRequest(
            0, 30, "9:16", "TIKTOK_POP", "Inter", 42, "#FFF", "#000", "#FFF", true, "[]", "Tampered", "Hook", "Cap", "Desc", "[]", "CTA"
        );

        var actUpdate = async () => await clipService.UpdateClipAsync(orgB.Id, clipA.Id, updateReq);
        await actUpdate.Should().ThrowAsync<NotFoundException>();

        // Act & Assert 3: Org B queries sources list -> must not contain Org A's source
        var sourcesOrgB = await searchService.GetDiscoveredSourcesAsync(orgB.Id);
        sourcesOrgB.Should().NotContain(s => s.Id == sourceA.Id);
    }
}
