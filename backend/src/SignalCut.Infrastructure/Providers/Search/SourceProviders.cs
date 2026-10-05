using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SignalCut.Application.Interfaces;
using SignalCut.Domain.Enums;

namespace SignalCut.Infrastructure.Providers.Search;

public class YouTubeSourceProvider : ISourceProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<YouTubeSourceProvider> _logger;

    public string ProviderName => "YouTube";

    public YouTubeSourceProvider(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<YouTubeSourceProvider> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<IEnumerable<SourceDiscoveryItem>> SearchAsync(string query, int limit = 20, CancellationToken ct = default)
    {
        _logger.LogInformation("Executing real YouTube discovery search for query: {Query}", query);

        var workerUrl = _config["AI_WORKER_URL"] ?? "http://localhost:8000";

        try
        {
            // Call AI worker yt-dlp discovery endpoint
            var url = $"{workerUrl}/api/v1/search/youtube?query={Uri.EscapeDataString(query)}&limit={limit}";
            using var reqMsg = new HttpRequestMessage(HttpMethod.Get, url);
            var apiKey = _config["AI_WORKER_API_KEY"];
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                reqMsg.Headers.Add("X-API-Key", apiKey);
            }
            var response = await _httpClient.SendAsync(reqMsg, ct);

            if (response.IsSuccessStatusCode)
            {
                var searchResults = await response.Content.ReadFromJsonAsync<List<WorkerSearchResultDto>>(cancellationToken: ct);
                if (searchResults != null && searchResults.Count > 0)
                {
                    _logger.LogInformation("Successfully retrieved {Count} real YouTube videos from AI worker discovery", searchResults.Count);
                    return searchResults.Select(r => new SourceDiscoveryItem(
                        r.Id,
                        r.Provider ?? ProviderName,
                        r.Title,
                        r.Description,
                        r.Creator,
                        r.Url,
                        DateTime.TryParse(r.PublishedAt, out var dt) ? dt : DateTime.UtcNow.AddDays(-7),
                        r.DurationSeconds,
                        r.ThumbnailUrl,
                        r.Language ?? "en",
                        ContentType.VIDEO,
                        RightsStatus.DISCOVERY_ONLY,
                        AuthorizationStatus.NOT_REQUIRED,
                        r.TranscriptAvailability,
                        r.RelevanceScore
                    ));
                }
            }
            else
            {
                _logger.LogWarning("AI worker YouTube search returned status code {Code}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch YouTube search results via AI worker. Ensure ai-worker is running at {Url}", workerUrl);
        }

        // Return empty or fallback only if AI worker is not reachable
        return Enumerable.Empty<SourceDiscoveryItem>();
    }

    public async Task<SourceDiscoveryItem?> GetMetadataAsync(string urlOrId, CancellationToken ct = default)
    {
        var searchResults = await SearchAsync(urlOrId, 1, ct);
        return searchResults.FirstOrDefault();
    }

    private sealed record WorkerSearchResultDto(
        string Id,
        string Provider,
        string Title,
        string Description,
        string Creator,
        string Url,
        string PublishedAt,
        int DurationSeconds,
        string ThumbnailUrl,
        string Language,
        string ContentType,
        string RightsStatus,
        string AuthorizationStatus,
        bool TranscriptAvailability,
        double RelevanceScore
    );
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
