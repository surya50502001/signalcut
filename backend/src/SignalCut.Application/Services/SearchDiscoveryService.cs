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
                IsAuthorizedForGeneration: source.RightsStatus == RightsStatus.USER_OWNED ||
                                           (source.RightsStatus == RightsStatus.USER_AUTHORIZED && source.AuthorizationStatus == AuthorizationStatus.VERIFIED) ||
                                           source.RightsStatus == RightsStatus.LICENSED ||
                                           source.RightsStatus == RightsStatus.PUBLIC_DOMAIN,
                source.RelevanceScore
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
            IsAuthorizedForGeneration: source.RightsStatus == RightsStatus.USER_OWNED ||
                                       (source.RightsStatus == RightsStatus.USER_AUTHORIZED && source.AuthorizationStatus == AuthorizationStatus.VERIFIED) ||
                                       source.RightsStatus == RightsStatus.LICENSED ||
                                       source.RightsStatus == RightsStatus.PUBLIC_DOMAIN,
            source.RelevanceScore
        );
    }

    public async Task<List<SourceDto>> GetDiscoveredSourcesAsync(Guid organizationId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var sources = await _context.Sources
            .Where(s => s.OrganizationId == organizationId)
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SourceDto(
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
                s.RightsStatus == RightsStatus.USER_OWNED ||
                (s.RightsStatus == RightsStatus.USER_AUTHORIZED && s.AuthorizationStatus == AuthorizationStatus.VERIFIED) ||
                s.RightsStatus == RightsStatus.LICENSED ||
                s.RightsStatus == RightsStatus.PUBLIC_DOMAIN,
                s.RelevanceScore
            ))
            .ToListAsync(ct);

        return sources;
    }
}
