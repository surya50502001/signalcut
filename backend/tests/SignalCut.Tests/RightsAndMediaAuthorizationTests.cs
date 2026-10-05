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
    public async Task DiscoveredYouTubeSource_CannotGenerateMoments_WithoutMediaUpload()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var rightsService = new RightsAuthorizationService(context, NullLogger<RightsAuthorizationService>.Instance);
        var walletService = new CreditWalletService(context, NullLogger<CreditWalletService>.Instance);
        var transcriptProvider = new TestTranscriptProvider(null);
        var llmProvider = new TestLanguageModelProvider();
        var momentService = new MomentService(
            context,
            llmProvider,
            transcriptProvider,
            walletService,
            rightsService,
            NullLogger<MomentService>.Instance
        );

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Seed wallet with 100 credits
        await walletService.AddCreditsAsync(orgId, 100m, CreditTransactionType.PURCHASE, null, "Initial test balance", null);

        var discoverySource = new Source
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Provider = "YouTube",
            Title = "Top Tech Trends",
            Url = "https://youtube.com/watch?v=sample123",
            RightsStatus = RightsStatus.DISCOVERY_ONLY,
            AuthorizationStatus = AuthorizationStatus.NOT_REQUIRED
        };

        context.Sources.Add(discoverySource);
        await context.SaveChangesAsync();

        // Act: Attempt to detect moments on a DISCOVERY_ONLY source without media upload
        var act = async () => await momentService.DetectMomentsAsync(orgId, userId, discoverySource.Id, MomentObjective.Educational);

        // Assert: Must throw UnauthorizedMediaException (mapping to 403 Forbidden)
        var ex = await act.Should().ThrowAsync<UnauthorizedMediaException>();
        ex.Which.SourceId.Should().Be(discoverySource.Id);
        ex.Which.Message.Should().Contain("Upload the video/audio you have permission to use to continue");
    }

    [Fact]
    public async Task ConfirmRightsAlone_OnDiscoveredYouTubeSource_StillBlocksMomentsAndClipCreation()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var rightsService = new RightsAuthorizationService(context, NullLogger<RightsAuthorizationService>.Instance);
        var walletService = new CreditWalletService(context, NullLogger<CreditWalletService>.Instance);
        var transcriptProvider = new TestTranscriptProvider(null);
        var llmProvider = new TestLanguageModelProvider();
        var momentService = new MomentService(
            context,
            llmProvider,
            transcriptProvider,
            walletService,
            rightsService,
            NullLogger<MomentService>.Instance
        );
        var clipService = new ClipService(
            context,
            rightsService,
            walletService,
            new TestVideoProcessor(),
            NullLogger<ClipService>.Instance
        );

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await walletService.AddCreditsAsync(orgId, 100m, CreditTransactionType.PURCHASE, null, "Initial balance", null);

        var source = new Source
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Provider = "YouTube",
            Title = "AI Agents Revolution",
            Url = "https://youtube.com/watch?v=aiagents456",
            RightsStatus = RightsStatus.DISCOVERY_ONLY,
            AuthorizationStatus = AuthorizationStatus.NOT_REQUIRED
        };

        context.Sources.Add(source);
        await context.SaveChangesAsync();

        // Act 1: User merely confirms rights via statement WITHOUT uploading media
        var confirmReq = new RightsConfirmationRequest(
            source.Id,
            RightsStatus.USER_AUTHORIZED,
            "I confirm that I own this content or have permission to use, edit, and publish it."
        );

        var confirmResponse = await rightsService.ConfirmRightsAsync(orgId, userId, confirmReq);

        // Assert: Rights confirmation alone does NOT authorize external URL processing
        confirmResponse.Success.Should().BeFalse();
        confirmResponse.RightsStatus.Should().Be(RightsStatus.DISCOVERY_ONLY);
        confirmResponse.AuthorizationStatus.Should().Be(AuthorizationStatus.PENDING);
        confirmResponse.Message.Should().Contain("Upload the video/audio you have permission to use to continue");

        // Source entity in database must still remain DISCOVERY_ONLY
        var dbSource = await context.Sources.FirstAsync(s => s.Id == source.Id);
        dbSource.RightsStatus.Should().Be(RightsStatus.DISCOVERY_ONLY);

        // Act 2 & Assert: Pipeline assertion must still fail
        var pipelineAct = async () => await rightsService.AssertCanEnterGenerationPipelineAsync(source.Id);
        await pipelineAct.Should().ThrowAsync<UnauthorizedMediaException>();

        // Act 3 & Assert: Moment detection must still be blocked
        var momentAct = async () => await momentService.DetectMomentsAsync(orgId, userId, source.Id, MomentObjective.Educational);
        await momentAct.Should().ThrowAsync<UnauthorizedMediaException>();

        // Act 4: Create a moment manually to test clip creation block
        var moment = new Moment
        {
            OrganizationId = orgId,
            SourceId = source.Id,
            StartTime = 10.0,
            EndTime = 40.0,
            TranscriptSnippet = "Sample insight"
        };
        context.Moments.Add(moment);
        await context.SaveChangesAsync();

        var clipAct = async () => await clipService.CreateClipFromMomentAsync(orgId, userId, new CreateClipRequest(moment.Id, null, 10.0, 40.0));
        await clipAct.Should().ThrowAsync<UnauthorizedMediaException>();
    }

    [Fact]
    public async Task UploadAuthorizedMedia_AndConfirmRights_UnblocksMomentExtractionAndClipGeneration()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var storage = new TestObjectStorage();
        var rightsService = new RightsAuthorizationService(context, NullLogger<RightsAuthorizationService>.Instance, storage);
        var walletService = new CreditWalletService(context, NullLogger<CreditWalletService>.Instance);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await walletService.AddCreditsAsync(orgId, 100m, CreditTransactionType.PURCHASE, null, "Initial balance", null);

        var source = new Source
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Provider = "YouTube",
            Title = "Building Distributed Systems",
            Url = "https://youtube.com/watch?v=distrib999",
            RightsStatus = RightsStatus.DISCOVERY_ONLY,
            AuthorizationStatus = AuthorizationStatus.NOT_REQUIRED
        };

        context.Sources.Add(source);
        await context.SaveChangesAsync();

        // Pipeline assertion initially fails
        var initialAssert = async () => await rightsService.AssertCanEnterGenerationPipelineAsync(source.Id);
        await initialAssert.Should().ThrowAsync<UnauthorizedMediaException>();

        // Act: User uploads authorized media and confirms legal statement
        var mp4Header = new byte[] { 0x00, 0x00, 0x00, 0x18, (byte)'f', (byte)'t', (byte)'y', (byte)'p', (byte)'i', (byte)'s', (byte)'o', (byte)'m' };
        var stream = new MemoryStream(mp4Header.Concat(System.Text.Encoding.UTF8.GetBytes("real audio video binary content")).ToArray());
        var confirmReq = new RightsConfirmationRequest(
            source.Id,
            RightsStatus.USER_AUTHORIZED,
            "I confirm that I own this content or have permission to use, edit, and publish it."
        );

        var uploadResult = await rightsService.UploadAndAuthorizeMediaAsync(
            orgId,
            userId,
            source.Id,
            stream,
            "distrib_systems.mp4",
            "video/mp4",
            confirmReq
        );

        // Assert: Upload succeeds and attaches MediaAsset
        uploadResult.Success.Should().BeTrue();
        uploadResult.RightsStatus.Should().Be(RightsStatus.USER_AUTHORIZED);
        uploadResult.AuthorizationStatus.Should().Be(AuthorizationStatus.VERIFIED);
        uploadResult.StorageUrl.Should().Contain("/storage/media/");

        // MediaAsset linked with tenant isolation
        var mediaAsset = await context.MediaAssets.FirstOrDefaultAsync(m => m.SourceId == source.Id);
        mediaAsset.Should().NotBeNull();
        mediaAsset!.OrganizationId.Should().Be(orgId);
        mediaAsset.ContentType.Should().Be("video/mp4");

        // Source entity updated to authorized
        var updatedSource = await context.Sources.FirstAsync(s => s.Id == source.Id);
        updatedSource.RightsStatus.Should().Be(RightsStatus.USER_AUTHORIZED);
        updatedSource.AuthorizationStatus.Should().Be(AuthorizationStatus.VERIFIED);
        updatedSource.Url.Should().Be(mediaAsset.StorageUrl);

        // Pipeline assertion passes!
        var pipelinePass = await rightsService.AssertCanEnterGenerationPipelineAsync(source.Id);
        pipelinePass.Should().BeTrue();

        // Moments service with a real transcript unblocks and executes cleanly
        var realTranscript = new TranscriptResult(
            "Distributed consensus protocols allow decoupled computing nodes to agree on a unified state machine.",
            "en",
            15,
            new List<TranscriptChunkResult>
            {
                new(0, 0, 15, "Distributed consensus protocols allow decoupled computing nodes to agree.", "Speaker 1", 0.99)
            },
            false,
            "Whisper"
        );
        var transcriptProvider = new TestTranscriptProvider(realTranscript);
        var llmProvider = new TestLanguageModelProvider();
        var momentService = new MomentService(context, llmProvider, transcriptProvider, walletService, rightsService, NullLogger<MomentService>.Instance);

        var moments = await momentService.DetectMomentsAsync(orgId, userId, source.Id, MomentObjective.Technical);
        moments.Should().NotBeEmpty();

        // Clip generation unblocks
        var clipService = new ClipService(context, rightsService, walletService, new TestVideoProcessor(), NullLogger<ClipService>.Instance);
        var clip = await clipService.CreateClipFromMomentAsync(orgId, userId, new CreateClipRequest(moments[0].Id, null, 0.0, 15.0));
        clip.Should().NotBeNull();
        clip.MomentId.Should().Be(moments[0].Id);
    }

    [Fact]
    public async Task RenderWorker_UsesUploadedFile_NotYouTubeUrl()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var rightsService = new RightsAuthorizationService(context, NullLogger<RightsAuthorizationService>.Instance);
        var walletService = new CreditWalletService(context, NullLogger<CreditWalletService>.Instance);
        var videoProcessor = new TestVideoProcessor();
        var clipService = new ClipService(context, rightsService, walletService, videoProcessor, NullLogger<ClipService>.Instance);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await walletService.AddCreditsAsync(orgId, 100m, CreditTransactionType.PURCHASE, null, "Initial balance", null);

        // A source with a YouTube URL but NO uploaded media asset
        var youtubeSource = new Source
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Provider = "YouTube",
            Title = "Podcast without upload",
            Url = "https://youtube.com/watch?v=unauthorized",
            RightsStatus = RightsStatus.DISCOVERY_ONLY
        };
        context.Sources.Add(youtubeSource);

        var moment = new Moment
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            SourceId = youtubeSource.Id,
            StartTime = 5.0,
            EndTime = 35.0,
            TranscriptSnippet = "Insight from podcast"
        };
        context.Moments.Add(moment);

        var clip = new Clip
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            MomentId = moment.Id,
            CreatedByUserId = userId,
            StartTime = 5.0,
            EndTime = 35.0
        };
        context.Clips.Add(clip);
        await context.SaveChangesAsync();

        // Act & Assert: Attempting to queue render for this clip MUST throw UnauthorizedMediaException
        var act = async () => await clipService.QueueRenderAsync(orgId, userId, clip.Id);
        await act.Should().ThrowAsync<UnauthorizedMediaException>();
    }

    [Fact]
    public async Task UnauthorizedOrMissingMedia_FailsCleanlyWithoutSyntheticFallbacks()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var storage = new TestObjectStorage();
        var rightsService = new RightsAuthorizationService(context, NullLogger<RightsAuthorizationService>.Instance, storage);
        var walletService = new CreditWalletService(context, NullLogger<CreditWalletService>.Instance);

        // Transcript provider returns NULL (e.g. video has no captions and cannot be transcribed)
        var transcriptProvider = new TestTranscriptProvider(null);
        var llmProvider = new TestLanguageModelProvider();
        var momentService = new MomentService(context, llmProvider, transcriptProvider, walletService, rightsService, NullLogger<MomentService>.Instance);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var initialWallet = await walletService.GetWalletAsync(orgId);
        var initialBalance = initialWallet.AvailableBalance;

        var source = new Source
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Provider = "UserUpload",
            Title = "Silent or Uncaptioned Media",
            Url = "/storage/media/silent.mp4",
            RightsStatus = RightsStatus.USER_OWNED,
            AuthorizationStatus = AuthorizationStatus.VERIFIED
        };
        context.Sources.Add(source);

        var mediaAsset = new MediaAsset
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            SourceId = source.Id,
            AssetType = "SOURCE_VIDEO",
            StorageKey = "media/silent.mp4",
            StorageUrl = "/storage/media/silent.mp4",
            ContentType = "video/mp4"
        };
        context.MediaAssets.Add(mediaAsset);
        await context.SaveChangesAsync();

        // Act: Attempt to detect moments when real transcript is unavailable
        var act = async () => await momentService.DetectMomentsAsync(orgId, userId, source.Id, MomentObjective.Educational);

        // Assert: Must fail cleanly with InvalidOperationException without generating fake synthetic transcript
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.Message.Should().Contain("Real media transcript or caption data is required");

        // Verify reserved credits were refunded to wallet
        var wallet = await walletService.GetWalletAsync(orgId);
        wallet.AvailableBalance.Should().Be(initialBalance);
        wallet.ReservedBalance.Should().Be(0m);
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

    [Fact]
    public async Task UserUploadSource_WithoutMediaAsset_IsBlockedFromPipeline()
    {
        // Arrange: Source has Provider="UserUpload" and RightsStatus=USER_OWNED, but no MediaAsset attached
        using var context = CreateInMemoryDbContext();
        var rightsService = new RightsAuthorizationService(context, NullLogger<RightsAuthorizationService>.Instance);

        var orgId = Guid.NewGuid();
        var source = new Source
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Provider = "UserUpload",
            ContentType = ContentType.USER_UPLOAD,
            Title = "User Upload Without File",
            RightsStatus = RightsStatus.USER_OWNED,
            AuthorizationStatus = AuthorizationStatus.VERIFIED
        };
        context.Sources.Add(source);
        await context.SaveChangesAsync();

        // Act & Assert: Must be BLOCKED because Provider=="UserUpload" alone does not grant pipeline access without an actual MediaAsset
        var act = async () => await rightsService.AssertCanEnterGenerationPipelineAsync(source.Id);
        var ex = await act.Should().ThrowAsync<UnauthorizedMediaException>();
        ex.Which.SourceId.Should().Be(source.Id);
    }

    [Fact]
    public async Task ExternalLicensedSource_WithoutMediaAsset_BlockedUnlessVerified()
    {
        // Arrange: Discovered licensed source with PENDING status and no MediaAsset
        using var context = CreateInMemoryDbContext();
        var rightsService = new RightsAuthorizationService(context, NullLogger<RightsAuthorizationService>.Instance);

        var orgId = Guid.NewGuid();
        var source = new Source
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Provider = "Podcast",
            Title = "Unverified Licensed Source",
            Url = "https://podcast.example.com/episode1",
            RightsStatus = RightsStatus.LICENSED,
            AuthorizationStatus = AuthorizationStatus.PENDING
        };
        context.Sources.Add(source);
        await context.SaveChangesAsync();

        // Act & Assert: Must throw UnauthorizedMediaException
        var act = async () => await rightsService.AssertCanEnterGenerationPipelineAsync(source.Id);
        await act.Should().ThrowAsync<UnauthorizedMediaException>();
    }

    [Fact]
    public async Task InvalidMediaContainerMagicBytes_IsRejected()
    {
        // Arrange: Client uploads a text/executable stream claiming to be .mp4
        using var context = CreateInMemoryDbContext();
        var storage = new TestObjectStorage();
        var rightsService = new RightsAuthorizationService(context, NullLogger<RightsAuthorizationService>.Instance, storage);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var source = new Source
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Title = "Target Source",
            RightsStatus = RightsStatus.DISCOVERY_ONLY
        };
        context.Sources.Add(source);
        await context.SaveChangesAsync();

        // Plain text bytes disguised as video/mp4
        var malformedStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("This is plain text and not a valid MP4 or media container!"));
        var confirmReq = new RightsConfirmationRequest(
            source.Id,
            RightsStatus.USER_AUTHORIZED,
            "I confirm that I own this content or have permission to use, edit, and publish it."
        );

        // Act & Assert: Must reject with ValidationException due to magic bytes mismatch
        var act = async () => await rightsService.UploadAndAuthorizeMediaAsync(
            orgId,
            userId,
            source.Id,
            malformedStream,
            "fake_video.mp4",
            "video/mp4",
            confirmReq
        );

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("FileContent");
    }

    [Fact]
    public async Task RenderWithoutMediaAsset_IsBlocked()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var rightsService = new RightsAuthorizationService(context, NullLogger<RightsAuthorizationService>.Instance);
        var walletService = new CreditWalletService(context, NullLogger<CreditWalletService>.Instance);
        var clipService = new ClipService(context, rightsService, walletService, new TestVideoProcessor(), NullLogger<ClipService>.Instance);

        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var source = new Source
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Title = "Source Without Asset",
            RightsStatus = RightsStatus.USER_OWNED,
            AuthorizationStatus = AuthorizationStatus.VERIFIED
        };
        context.Sources.Add(source);

        var moment = new Moment
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            SourceId = source.Id,
            StartTime = 10.0,
            EndTime = 30.0,
            TranscriptSnippet = "Sample insight",
            SuggestedTitle = "Test Clip",
            SuggestedHook = "Test Hook"
        };
        context.Moments.Add(moment);

        var clip = new Clip
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            MomentId = moment.Id,
            CreatedByUserId = userId,
            StartTime = 10.0,
            EndTime = 30.0,
            RenderStatus = JobStatus.CREATED
        };
        context.Clips.Add(clip);
        await context.SaveChangesAsync();

        // Act & Assert: Attempting to queue render for a source without an authorized MediaAsset must throw UnauthorizedMediaException
        var act = async () => await clipService.QueueRenderAsync(orgId, userId, clip.Id);
        await act.Should().ThrowAsync<UnauthorizedMediaException>();
    }

    // --- Test Stubs ---
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

    private class TestTranscriptProvider : ITranscriptProvider
    {
        private readonly TranscriptResult? _result;
        public TestTranscriptProvider(TranscriptResult? result) => _result = result;
        public Task<TranscriptResult?> FetchExistingTranscriptAsync(string sourceUrl, CancellationToken ct = default)
            => Task.FromResult(_result);
    }

    private class TestLanguageModelProvider : ILanguageModelProvider
    {
        public string ProviderName => "TestLLM";
        public Task<List<MomentCandidate>> FindMomentsAsync(string transcriptText, string topicQuery, MomentObjective objective, CancellationToken ct = default)
            => Task.FromResult(new List<MomentCandidate>
            {
                new(10.0, 40.0, "Insight snippet", "High signal", 0.95, 0.92, 0.90, 93.0, "Speaker 1", 0.98, objective, "Hook", "Title", "Caption", "Desc", new List<string> { "#Tech" }, "Save clip")
            });

        public Task<(string Hook, string Title, string Caption, string Description, List<string> Hashtags, string Cta)> GenerateCopyAsync(string momentTranscript, string contextTopic, CancellationToken ct = default)
            => Task.FromResult(("Hook", "Title", "Caption", "Desc", new List<string> { "#Tech" }, "Save clip"));
    }

    private class TestVideoProcessor : IVideoProcessor
    {
        public Task<RenderVideoResult> RenderClipAsync(RenderVideoRequest request, Action<int>? onProgress = null, CancellationToken ct = default)
            => Task.FromResult(new RenderVideoResult(true, "renders/test.mp4", "/storage/renders/test.mp4", "/storage/renders/thumb.jpg", 15.0, null));
    }
}

