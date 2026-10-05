using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SignalCut.Application.Common;
using SignalCut.Application.DTOs;
using SignalCut.Application.Interfaces;
using SignalCut.Domain.Entities;
using SignalCut.Domain.Enums;

namespace SignalCut.Application.Services;

public class SearchDiscoveryService : ISearchDiscoveryService
{
    private readonly IApplicationDbContext _context;
    private readonly ISearchProvider _searchProvider;
    private readonly ILogger<SearchDiscoveryService> _logger;

    public SearchDiscoveryService(
        IApplicationDbContext context,
        ISearchProvider searchProvider,
        ILogger<SearchDiscoveryService> logger)
    {
        _context = context;
        _searchProvider = searchProvider;
        _logger = logger;
    }

    public async Task<SearchResultDto> SearchTopicAsync(Guid organizationId, Guid userId, SearchQueryRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            throw new ValidationException("Query", "Search query cannot be empty.");
        }

        var trimmedQuery = request.Query.Trim();
        _logger.LogInformation("Processing search topic: '{Query}' for org {OrgId}", trimmedQuery, organizationId);

        // 1. Query parsing & entity extraction
        var (topics, intent) = await _searchProvider.ParseQueryAsync(trimmedQuery, ct);

        // 2. Discover from all providers (YouTube, Podcasts, Conferences, User Media, Licensed Providers)
        var rawResults = await _searchProvider.SearchAllAsync(trimmedQuery, request.Limit, ct);

        // 3. Deduplicate by URL and Title similarity
        var deduplicated = rawResults
            .GroupBy(r => (r.Url ?? r.ExternalId).ToLowerInvariant())
            .Select(g => g.First())
            .OrderByDescending(r => r.RelevanceScore)
            .Take(request.Limit)
            .ToList();

        // 4. Create Search entity
        var search = new Search
        {
            OrganizationId = organizationId,
            UserId = userId,
            ProjectId = request.ProjectId,
            RawQuery = trimmedQuery,
            ParsedTopicsJson = System.Text.Json.JsonSerializer.Serialize(topics),
            Intent = intent,
            ResultCount = deduplicated.Count
        };

        _context.Searches.Add(search);
        await _context.SaveChangesAsync(ct);

        // 5. Persist Source entities for the organization
        var sourceDtos = new List<SourceDto>();

        foreach (var item in deduplicated)
        {
            // Check if already existing in org
            var existing = await _context.Sources
                .Include(s => s.MediaAssets)
                .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && (s.ExternalSourceId == item.ExternalId || s.Url == item.Url), ct);

            Source source;
            if (existing != null)
            {
                source = existing;
                source.RelevanceScore = item.RelevanceScore;
                source.SearchId = search.Id;
            }
            else
            {
                source = new Source
                {
                    OrganizationId = organizationId,
                    SearchId = search.Id,
                    ProjectId = request.ProjectId,
                    ExternalSourceId = item.ExternalId,
                    Provider = item.Provider,
                    Title = item.Title,
                    Description = item.Description,
                    Creator = item.Creator,
                    Url = item.Url,
                    PublishedAt = item.PublishedAt,
                    DurationSeconds = item.DurationSeconds,
                    ThumbnailUrl = item.ThumbnailUrl,
                    Language = item.Language,
                    ContentType = item.ContentType,
                    // By default, public discoveries are DISCOVERY_ONLY to protect copyright
                    RightsStatus = item.RightsStatus,
                    AuthorizationStatus = item.AuthorizationStatus,
                    TranscriptAvailability = item.TranscriptAvailability,
                    RelevanceScore = item.RelevanceScore
                };
                _context.Sources.Add(source);
            }

            sourceDtos.Add(new SourceDto(
                source.Id,
                source.ExternalSourceId,
                source.Provider,
                source.Title,
                source.Description,
                source.Creator,
                source.Url,
                source.PublishedAt,
                source.DurationSeconds,
                source.ThumbnailUrl,
                source.Language,
                source.ContentType,
                source.RightsStatus,
                source.AuthorizationStatus,
                source.TranscriptAvailability,
                IsAuthorizedForGeneration: CheckIsAuthorizedForGeneration(source),
                source.RelevanceScore,
                BestMomentTimestamp: FormatBestMomentTimestamp(source.DurationSeconds, source.RelevanceScore),
                Episode: ExtractEpisode(source.Title)
            ));
        }

        await _context.SaveChangesAsync(ct);

        return new SearchResultDto(
            search.Id,
            search.RawQuery,
            topics,
            intent,
            sourceDtos
        );
    }

    public async Task<SourceDto?> GetSourceByIdAsync(Guid organizationId, Guid sourceId, CancellationToken ct = default)
    {
        var source = await _context.Sources
            .Include(s => s.MediaAssets)
            .FirstOrDefaultAsync(s => s.Id == sourceId && s.OrganizationId == organizationId, ct);

        if (source == null) return null;

        return new SourceDto(
            source.Id,
            source.ExternalSourceId,
            source.Provider,
            source.Title,
            source.Description,
            source.Creator,
            source.Url,
            source.PublishedAt,
            source.DurationSeconds,
            source.ThumbnailUrl,
            source.Language,
            source.ContentType,
            source.RightsStatus,
            source.AuthorizationStatus,
            source.TranscriptAvailability,
            IsAuthorizedForGeneration: CheckIsAuthorizedForGeneration(source),
            source.RelevanceScore,
            BestMomentTimestamp: FormatBestMomentTimestamp(source.DurationSeconds, source.RelevanceScore),
            Episode: ExtractEpisode(source.Title)
        );
    }

    public async Task<List<SourceDto>> GetDiscoveredSourcesAsync(Guid organizationId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var sources = await _context.Sources
            .Include(s => s.MediaAssets)
            .Where(s => s.OrganizationId == organizationId)
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return sources.Select(s => new SourceDto(
            s.Id,
            s.ExternalSourceId,
            s.Provider,
            s.Title,
            s.Description,
            s.Creator,
            s.Url,
            s.PublishedAt,
            s.DurationSeconds,
            s.ThumbnailUrl,
            s.Language,
            s.ContentType,
            s.RightsStatus,
            s.AuthorizationStatus,
            s.TranscriptAvailability,
            CheckIsAuthorizedForGeneration(s),
            s.RelevanceScore,
            BestMomentTimestamp: FormatBestMomentTimestamp(s.DurationSeconds, s.RelevanceScore),
            Episode: ExtractEpisode(s.Title)
        )).ToList();
    }

    private static bool CheckIsAuthorizedForGeneration(Source s)
    {
        if (s.RightsStatus == RightsStatus.DISCOVERY_ONLY || s.RightsStatus == RightsStatus.BLOCKED || s.RightsStatus == RightsStatus.UNKNOWN)
        {
            return false;
        }

        var isDirectUserUpload = s.Provider.Equals("UserUpload", StringComparison.OrdinalIgnoreCase) || s.ContentType == ContentType.USER_UPLOAD;
        var hasUploadedMedia = s.MediaAssets != null && s.MediaAssets.Any(m => !string.IsNullOrEmpty(m.StorageUrl) || !string.IsNullOrEmpty(m.StorageKey));
        var isLicensedDirect = (s.RightsStatus == RightsStatus.LICENSED || s.RightsStatus == RightsStatus.PUBLIC_DOMAIN) && s.AuthorizationStatus == AuthorizationStatus.VERIFIED;

        var hasAuthorizedMedia = isDirectUserUpload || hasUploadedMedia || isLicensedDirect;
        if (!hasAuthorizedMedia) return false;

        return s.RightsStatus switch
        {
            RightsStatus.USER_OWNED => true,
            RightsStatus.USER_AUTHORIZED => s.AuthorizationStatus == AuthorizationStatus.VERIFIED,
            RightsStatus.LICENSED => s.AuthorizationStatus == AuthorizationStatus.VERIFIED,
            RightsStatus.PUBLIC_DOMAIN => true,
            _ => false
        };
    }

    private static string? ExtractEpisode(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;
        var match = System.Text.RegularExpressions.Regex.Match(
            title,
            @"(?:Episode|Ep\.?|#)\s*(\d+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? $"Episode {match.Groups[1].Value}" : null;
    }

    private static string FormatBestMomentTimestamp(double durationSeconds, double relevanceScore)
    {
        if (durationSeconds <= 0) return "00:00";
        var targetSecs = Math.Max(30, Math.Min(durationSeconds * 0.35, durationSeconds - 30));
        var mins = (int)(targetSecs / 60);
        var secs = (int)(targetSecs % 60);
        return $"{mins:D2}:{secs:D2}";
    }
}

