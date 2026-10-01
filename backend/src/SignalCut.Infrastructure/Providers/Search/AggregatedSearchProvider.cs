using Microsoft.Extensions.Logging;
using SignalCut.Application.Interfaces;

namespace SignalCut.Infrastructure.Providers.Search;

public class AggregatedSearchProvider : ISearchProvider
{
    private readonly IEnumerable<ISourceProvider> _sourceProviders;
    private readonly ILogger<AggregatedSearchProvider> _logger;

    public AggregatedSearchProvider(
        IEnumerable<ISourceProvider> sourceProviders,
        ILogger<AggregatedSearchProvider> logger)
    {
        _sourceProviders = sourceProviders;
        _logger = logger;
    }

    public Task<(List<string> Topics, string Intent)> ParseQueryAsync(string query, CancellationToken ct = default)
    {
        var cleaned = query.Trim().Replace("?", "").Replace("\"", "");
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "what", "are", "saying", "about", "the", "best", "arguments", "latest", "discussions", "interviews", "how", "why", "in", "is", "a", "an", "and", "of", "to", "for"
        };

        var tokens = cleaned.Split(new[] { ' ', ',', '-', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => !stopWords.Contains(t) && t.Length > 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var topics = new List<string>();
        if (tokens.Count > 0)
        {
            topics.Add(cleaned); // Full concept
            topics.AddRange(tokens.Take(3));
        }
        else
        {
            topics.Add(cleaned);
        }

        string intent = "Discovery & Repurposing";
        if (query.Contains("CEO", StringComparison.OrdinalIgnoreCase) || query.Contains("founder", StringComparison.OrdinalIgnoreCase))
        {
            intent = "Executive Opinions & Leadership Commentary";
        }
        else if (query.Contains("argument", StringComparison.OrdinalIgnoreCase) || query.Contains("versus", StringComparison.OrdinalIgnoreCase) || query.Contains("vs", StringComparison.OrdinalIgnoreCase))
        {
            intent = "Contrarian Perspectives & Debates";
        }
        else if (query.Contains("latest", StringComparison.OrdinalIgnoreCase) || query.Contains("trend", StringComparison.OrdinalIgnoreCase))
        {
            intent = "Emerging Trends & Newsworthy Signals";
        }

        return Task.FromResult((topics, intent));
    }

    public async Task<IEnumerable<SourceDiscoveryItem>> SearchAllAsync(string query, int limit = 30, CancellationToken ct = default)
    {
        _logger.LogInformation("Aggregated search across {Count} source providers for: {Query}", _sourceProviders.Count(), query);

        var tasks = _sourceProviders.Select(p => p.SearchAsync(query, limit, ct)).ToList();
        var results = await Task.WhenAll(tasks);

        var flattened = results.SelectMany(r => r).ToList();

        // Calculate relevance scoring based on query terms
        var queryTerms = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var item in flattened)
        {
            double matchCount = queryTerms.Count(t => item.Title.ToLowerInvariant().Contains(t) || item.Description.ToLowerInvariant().Contains(t));
            double boost = (matchCount / Math.Max(1, queryTerms.Length)) * 0.15;
            // Bound score between 0.70 and 0.99
            var adjustedScore = Math.Min(0.99, Math.Max(0.70, item.RelevanceScore + boost));
        }

        return flattened
            .OrderByDescending(r => r.RelevanceScore)
            .Take(limit);
    }
}
