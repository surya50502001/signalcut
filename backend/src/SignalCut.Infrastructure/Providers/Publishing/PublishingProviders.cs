using Microsoft.Extensions.Logging;
using SignalCut.Application.Interfaces;
using SignalCut.Domain.Enums;

namespace SignalCut.Infrastructure.Providers.Publishing;

public class YouTubePublishingProvider : IPublishingProvider
{
    private readonly ILogger<YouTubePublishingProvider> _logger;
    public PublishingPlatform Platform => PublishingPlatform.YOUTUBE;

    public YouTubePublishingProvider(ILogger<YouTubePublishingProvider> logger)
    {
        _logger = logger;
    }

    public Task<PublishPostResult> PublishAsync(PublishPostRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Publishing short to YouTube. Title: {Title}", request.Title);
        var postId = $"yt_short_{Guid.NewGuid():N}";
        return Task.FromResult(new PublishPostResult(
            Success: true,
            ExternalPostId: postId,
            ExternalPostUrl: $"https://youtube.com/shorts/{postId}",
            ErrorMessage: null
        ));
    }
}

public class LinkedInPublishingProvider : IPublishingProvider
{
    public PublishingPlatform Platform => PublishingPlatform.LINKEDIN;

    public Task<PublishPostResult> PublishAsync(PublishPostRequest request, CancellationToken ct = default)
    {
        var postId = $"urn:li:share:{Guid.NewGuid():N}";
        return Task.FromResult(new PublishPostResult(
            Success: true,
            ExternalPostId: postId,
            ExternalPostUrl: $"https://www.linkedin.com/feed/update/{postId}",
            ErrorMessage: null
        ));
    }
}

public class InstagramPublishingProvider : IPublishingProvider
{
    public PublishingPlatform Platform => PublishingPlatform.INSTAGRAM;

    public Task<PublishPostResult> PublishAsync(PublishPostRequest request, CancellationToken ct = default)
    {
        var postId = $"ig_reel_{Guid.NewGuid():N}";
        return Task.FromResult(new PublishPostResult(
            Success: true,
            ExternalPostId: postId,
            ExternalPostUrl: $"https://www.instagram.com/reel/{postId}",
            ErrorMessage: null
        ));
    }
}

public class TikTokPublishingProvider : IPublishingProvider
{
    public PublishingPlatform Platform => PublishingPlatform.TIKTOK;

    public Task<PublishPostResult> PublishAsync(PublishPostRequest request, CancellationToken ct = default)
    {
        var postId = $"tt_video_{Guid.NewGuid():N}";
        return Task.FromResult(new PublishPostResult(
            Success: true,
            ExternalPostId: postId,
            ExternalPostUrl: $"https://www.tiktok.com/@creator/video/{postId}",
            ErrorMessage: null
        ));
    }
}

public class XPublishingProvider : IPublishingProvider
{
    private readonly ILogger<XPublishingProvider> _logger;
    public PublishingPlatform Platform => PublishingPlatform.X;

    public XPublishingProvider(ILogger<XPublishingProvider> logger)
    {
        _logger = logger;
    }

    public Task<PublishPostResult> PublishAsync(PublishPostRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Publishing post/video to X (Twitter). Title: {Title}", request.Title);
        var postId = $"x_tweet_{Guid.NewGuid():N}";
        return Task.FromResult(new PublishPostResult(
            Success: true,
            ExternalPostId: postId,
            ExternalPostUrl: $"https://x.com/creator/status/{postId}",
            ErrorMessage: null
        ));
    }
}

