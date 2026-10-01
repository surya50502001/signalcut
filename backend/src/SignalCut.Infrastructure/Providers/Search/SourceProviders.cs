using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SignalCut.Application.Interfaces;
using SignalCut.Domain.Enums;

namespace SignalCut.Infrastructure.Providers.Search;

public class YouTubeSourceProvider : ISourceProvider
{
    private readonly ILogger<YouTubeSourceProvider> _logger;

    public string ProviderName => "YouTube";

    public YouTubeSourceProvider(ILogger<YouTubeSourceProvider> logger)
    {
        _logger = logger;
    }

    public Task<IEnumerable<SourceDiscoveryItem>> SearchAsync(string query, int limit = 20, CancellationToken ct = default)
    {
        _logger.LogInformation("Searching YouTube provider for query: {Query}", query);

        // Generates realistic discovered content based on topic query
        var sanitizedTopic = query.Replace("?", "").Trim();
        var items = new List<SourceDiscoveryItem>
        {
            new(
                $"yt_{Math.Abs(query.GetHashCode()) % 100000 + 101}",
                ProviderName,
                $"{sanitizedTopic} - In-Depth Keynote & Discussion",
                $"Full technical keynote exploring {sanitizedTopic}, the architectural implications, practical case studies, and engineering paradigms.",
                "Tech Pioneers & Leaders",
                $"https://www.youtube.com/watch?v=sc_{Math.Abs(query.GetHashCode()) % 90000 + 10000}",
                DateTime.UtcNow.AddDays(-14),
                3120, // 52 mins
                "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?w=800&q=80",
                "en",
                ContentType.VIDEO,
                RightsStatus.DISCOVERY_ONLY, // Default discovery status to protect copyright
                AuthorizationStatus.NOT_REQUIRED,
                TranscriptAvailability: true,
                RelevanceScore: 0.96
            ),
            new(
                $"yt_{Math.Abs(query.GetHashCode()) % 100000 + 102}",
                ProviderName,
                $"The Real Truth About {sanitizedTopic} Explained",
                $"An analytical breakdown of {sanitizedTopic} covering contrarian viewpoints, industry data, and future outlook.",
                "Engineering Insights Channel",
                $"https://www.youtube.com/watch?v=sc_{Math.Abs(query.GetHashCode()) % 90000 + 20000}",
                DateTime.UtcNow.AddDays(-7),
                1840, // ~30 mins
                "https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?w=800&q=80",
                "en",
                ContentType.VIDEO,
                RightsStatus.DISCOVERY_ONLY,
                AuthorizationStatus.NOT_REQUIRED,
                TranscriptAvailability: true,
                RelevanceScore: 0.91
            )
        };

        return Task.FromResult<IEnumerable<SourceDiscoveryItem>>(items);
    }

    public Task<SourceDiscoveryItem?> GetMetadataAsync(string urlOrId, CancellationToken ct = default)
    {
        return Task.FromResult<SourceDiscoveryItem?>(new SourceDiscoveryItem(
            "yt_sample",
            ProviderName,
            "Sample YouTube Video",
            "Discovered sample video metadata.",
            "Creator Studio",
            urlOrId,
            DateTime.UtcNow.AddDays(-3),
            1200,
            "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?w=800&q=80",
            "en",
            ContentType.VIDEO,
            RightsStatus.DISCOVERY_ONLY,
            AuthorizationStatus.NOT_REQUIRED,
            true,
            0.9
        ));
    }
}

public class PodcastSourceProvider : ISourceProvider
{
    private readonly ILogger<PodcastSourceProvider> _logger;

    public string ProviderName => "Podcast";

    public PodcastSourceProvider(ILogger<PodcastSourceProvider> logger)
    {
        _logger = logger;
    }

    public Task<IEnumerable<SourceDiscoveryItem>> SearchAsync(string query, int limit = 20, CancellationToken ct = default)
    {
        _logger.LogInformation("Searching Podcast feeds for query: {Query}", query);

        var sanitizedTopic = query.Replace("?", "").Trim();
        var items = new List<SourceDiscoveryItem>
        {
            new(
                $"pod_{Math.Abs(query.GetHashCode()) % 100000 + 201}",
                ProviderName,
                $"The Deep Dive: {sanitizedTopic} Unpacked with Founders",
                $"Episode #142: Detailed conversational interview examining {sanitizedTopic} with leading startup founders and domain experts.",
                "Founders & Builders Podcast",
                $"https://podcasts.signalcut.app/episodes/{Math.Abs(query.GetHashCode()) % 90000 + 30000}",
                DateTime.UtcNow.AddDays(-5),
                2840,
                "https://images.unsplash.com/photo-1590602847861-f357a9332bbc?w=800&q=80",
                "en",
                ContentType.PODCAST,
                RightsStatus.DISCOVERY_ONLY,
                AuthorizationStatus.NOT_REQUIRED,
                TranscriptAvailability: true,
                RelevanceScore: 0.94
            )
        };

        return Task.FromResult<IEnumerable<SourceDiscoveryItem>>(items);
    }

    public Task<SourceDiscoveryItem?> GetMetadataAsync(string urlOrId, CancellationToken ct = default)
    {
        return Task.FromResult<SourceDiscoveryItem?>(null);
    }
}

public class WebVideoSourceProvider : ISourceProvider
{
    public string ProviderName => "Conferences & Interviews";

    public Task<IEnumerable<SourceDiscoveryItem>> SearchAsync(string query, int limit = 20, CancellationToken ct = default)
    {
        var sanitizedTopic = query.Replace("?", "").Trim();
        var items = new List<SourceDiscoveryItem>
        {
            new(
                $"conf_{Math.Abs(query.GetHashCode()) % 100000 + 301}",
                ProviderName,
                $"Global Summit Panel: The Future of {sanitizedTopic}",
                $"Executive panel discussion discussing the real-world operational challenges of {sanitizedTopic}.",
                "Global Tech Summit 2026",
                $"https://conferences.signalcut.app/sessions/{Math.Abs(query.GetHashCode()) % 90000 + 40000}",
                DateTime.UtcNow.AddDays(-20),
                3600,
                "https://images.unsplash.com/photo-1475721027785-f74eccf877e2?w=800&q=80",
                "en",
                ContentType.CONFERENCE,
                RightsStatus.DISCOVERY_ONLY,
                AuthorizationStatus.NOT_REQUIRED,
                TranscriptAvailability: true,
                RelevanceScore: 0.88
            )
        };

        return Task.FromResult<IEnumerable<SourceDiscoveryItem>>(items);
    }

    public Task<SourceDiscoveryItem?> GetMetadataAsync(string urlOrId, CancellationToken ct = default)
    {
        return Task.FromResult<SourceDiscoveryItem?>(null);
    }
}

public class UserUploadMediaProvider : ISourceProvider
{
    public string ProviderName => "User Connected Media";

    public Task<IEnumerable<SourceDiscoveryItem>> SearchAsync(string query, int limit = 20, CancellationToken ct = default)
    {
        // User uploads are inherently USER_OWNED and verified
        var items = new List<SourceDiscoveryItem>
        {
            new(
                "user_media_demo_01",
                ProviderName,
                $"Internal Recorded Demo: {query}",
                $"Your connected studio recording regarding {query}.",
                "Your Workspace",
                "https://cdn.signalcut.app/demo/internal_recording.mp4",
                DateTime.UtcNow.AddHours(-12),
                920,
                "https://images.unsplash.com/photo-1534447677768-be436bb09401?w=800&q=80",
                "en",
                ContentType.USER_UPLOAD,
                RightsStatus.USER_OWNED, // Pre-authorized because user owns this media!
                AuthorizationStatus.VERIFIED,
                TranscriptAvailability: true,
                RelevanceScore: 0.99
            )
        };

        return Task.FromResult<IEnumerable<SourceDiscoveryItem>>(items);
    }

    public Task<SourceDiscoveryItem?> GetMetadataAsync(string urlOrId, CancellationToken ct = default)
    {
        return Task.FromResult<SourceDiscoveryItem?>(null);
    }
}
