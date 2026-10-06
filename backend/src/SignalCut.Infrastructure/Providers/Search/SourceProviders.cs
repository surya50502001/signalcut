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
    private readonly HttpClient _httpClient;
    private readonly ILogger<PodcastSourceProvider> _logger;

    public string ProviderName => "Podcast";

    public PodcastSourceProvider(HttpClient httpClient, ILogger<PodcastSourceProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<SourceDiscoveryItem>> SearchAsync(string query, int limit = 20, CancellationToken ct = default)
    {
        _logger.LogInformation("Searching real Podcast feeds via iTunes API for query: {Query}", query);

        try
        {
            var searchUrl = $"https://itunes.apple.com/search?term={Uri.EscapeDataString(query)}&media=podcast&entity=podcastEpisode&limit={Math.Min(limit, 15)}";
            var response = await _httpClient.GetAsync(searchUrl, ct);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadFromJsonAsync<ItunesSearchResponse>(cancellationToken: ct);
                if (content?.Results != null && content.Results.Count > 0)
                {
                    var items = new List<SourceDiscoveryItem>();
                    for (int i = 0; i < content.Results.Count; i++)
                    {
                        var ep = content.Results[i];
                        var durationSec = (int)((ep.TrackTimeMillis ?? 1800000) / 1000);
                        var pubDate = DateTime.TryParse(ep.ReleaseDate, out var dt) ? dt : DateTime.UtcNow.AddDays(-i);
                        var episodeUrl = ep.EpisodeUrl ?? ep.TrackViewUrl ?? $"https://podcasts.apple.com/podcast/id{ep.CollectionId}";
                        var thumbUrl = ep.ArtworkUrl600 ?? ep.ArtworkUrl100 ?? "https://images.unsplash.com/photo-1590602847861-f357a9332bbc?w=800&q=80";
                        var score = Math.Round(Math.Max(0.70, 0.95 - (i * 0.03)), 2);

                        items.Add(new SourceDiscoveryItem(
                            $"pod_{ep.TrackId ?? ep.CollectionId ?? (long)i}",
                            ProviderName,
                            ep.TrackName ?? "Untitled Podcast Episode",
                            ep.Description ?? $"Discussion from {ep.CollectionName ?? "Podcast"}.",
                            ep.CollectionName ?? ep.ArtistName ?? "Podcast Creator",
                            episodeUrl,
                            pubDate,
                            durationSec,
                            thumbUrl,
                            "en",
                            ContentType.PODCAST,
                            RightsStatus.DISCOVERY_ONLY,
                            AuthorizationStatus.NOT_REQUIRED,
                            TranscriptAvailability: true,
                            RelevanceScore: score
                        ));
                    }
                    return items;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to search real iTunes podcasts for '{Query}'", query);
        }

        return Enumerable.Empty<SourceDiscoveryItem>();
    }

    public Task<SourceDiscoveryItem?> GetMetadataAsync(string urlOrId, CancellationToken ct = default)
    {
        return Task.FromResult<SourceDiscoveryItem?>(null);
    }

    private sealed class ItunesSearchResponse
    {
        public int ResultCount { get; set; }
        public List<ItunesEpisodeItem>? Results { get; set; }
    }

    private sealed class ItunesEpisodeItem
    {
        public long? TrackId { get; set; }
        public long? CollectionId { get; set; }
        public string? TrackName { get; set; }
        public string? CollectionName { get; set; }
        public string? ArtistName { get; set; }
        public string? Description { get; set; }
        public string? EpisodeUrl { get; set; }
        public string? TrackViewUrl { get; set; }
        public string? ArtworkUrl600 { get; set; }
        public string? ArtworkUrl100 { get; set; }
        public string? ReleaseDate { get; set; }
        public long? TrackTimeMillis { get; set; }
    }
}

public class WebVideoSourceProvider : ISourceProvider
{
    public string ProviderName => "Conferences & Interviews";

    public Task<IEnumerable<SourceDiscoveryItem>> SearchAsync(string query, int limit = 20, CancellationToken ct = default)
    {
        // No synthetic mock data — only genuine indexed video sources
        return Task.FromResult(Enumerable.Empty<SourceDiscoveryItem>());
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
        // No fake demo video — user uploads are retrieved from the workspace database
        return Task.FromResult(Enumerable.Empty<SourceDiscoveryItem>());
    }

    public Task<SourceDiscoveryItem?> GetMetadataAsync(string urlOrId, CancellationToken ct = default)
    {
        return Task.FromResult<SourceDiscoveryItem?>(null);
    }
}
