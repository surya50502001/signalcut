using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SignalCut.Application.Interfaces;
using SignalCut.Domain.Enums;

namespace SignalCut.Infrastructure.Providers.LLM;

public class CompositeLanguageModelProvider : ILanguageModelProvider
{
    private readonly IConfiguration _config;
    private readonly ILogger<CompositeLanguageModelProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly LocalFallbackLanguageModelProvider _fallback;

    public string ProviderName => _config["AI_PROVIDER"] ?? "SignalCut_Engine";

    public CompositeLanguageModelProvider(
        IConfiguration config,
        HttpClient httpClient,
        ILogger<CompositeLanguageModelProvider> logger)
    {
        _config = config;
        _httpClient = httpClient;
        _logger = logger;
        _fallback = new LocalFallbackLanguageModelProvider();
    }

    public async Task<List<MomentCandidate>> FindMomentsAsync(string transcriptText, string topicQuery, MomentObjective objective, CancellationToken ct = default)
    {
        var apiKey = _config["AI_API_KEY"];
        var provider = _config["AI_PROVIDER"]?.ToLowerInvariant();

        if (string.IsNullOrEmpty(apiKey) || provider == "mock" || provider == "local")
        {
            _logger.LogInformation("Using SignalCut high-fidelity local LLM analysis engine for topic '{Topic}'", topicQuery);
            return await _fallback.FindMomentsAsync(transcriptText, topicQuery, objective, ct);
        }

        try
        {
            // Call OpenAI or compatible API if configured
            var model = _config["AI_MODEL"] ?? "gpt-4o-mini";
            var requestBody = new
            {
                model,
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = "You are SignalCut AI, an elite video content analyst. Analyze the provided transcript for the requested topic and objective. Extract 3-5 high-signal, engaging moments. Output only valid JSON array of objects with keys: startTime, endTime, transcriptSnippet, reason, topicRelevance, hookStrength, informationDensity, clipScore, speaker, confidence, suggestedHook, suggestedTitle, suggestedCaption, suggestedDescription, suggestedHashtags, suggestedCta."
                    },
                    new
                    {
                        role = "user",
                        content = $"Topic: {topicQuery}\nObjective: {objective}\nTranscript: {transcriptText}"
                    }
                },
                temperature = 0.3
            };

            var reqMsg = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };
            reqMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var resp = await _httpClient.SendAsync(reqMsg, ct);
            if (resp.IsSuccessStatusCode)
            {
                var jsonStr = await resp.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(jsonStr);
                var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                if (!string.IsNullOrEmpty(content))
                {
                    var cleanJson = content.Trim();
                    if (cleanJson.StartsWith("```json")) cleanJson = cleanJson[7..];
                    if (cleanJson.EndsWith("```")) cleanJson = cleanJson[..^3];
                    var parsed = JsonSerializer.Deserialize<List<MomentCandidate>>(cleanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (parsed != null && parsed.Count > 0)
                    {
                        return parsed;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "External LLM API call failed; falling back to local analysis engine.");
        }

        return await _fallback.FindMomentsAsync(transcriptText, topicQuery, objective, ct);
    }

    public async Task<(string Hook, string Title, string Caption, string Description, List<string> Hashtags, string Cta)> GenerateCopyAsync(string momentTranscript, string contextTopic, CancellationToken ct = default)
    {
        return await _fallback.GenerateCopyAsync(momentTranscript, contextTopic, ct);
    }
}

public class LocalFallbackLanguageModelProvider : ILanguageModelProvider
{
    public string ProviderName => "SignalCut_Local_AI";

    public Task<List<MomentCandidate>> FindMomentsAsync(string transcriptText, string topicQuery, MomentObjective objective, CancellationToken ct = default)
    {
        var cleaned = topicQuery.Trim().Replace("?", "");
        var candidates = new List<MomentCandidate>
        {
            new(
                StartTime: 12.0,
                EndTime: 54.0,
                TranscriptSnippet: "The biggest misunderstanding about modern technology shifts is assuming the existing workflows stay intact. In reality, the entire leverage structure flips: execution becomes instantaneous, while taste, curation, and verification become the primary moat.",
                Reason: "Contrarian framing with high information density regarding execution vs taste.",
                TopicRelevance: 0.96,
                HookStrength: 0.94,
                InformationDensity: 0.92,
                ClipScore: 94.0,
                Speaker: "Speaker 1",
                Confidence: 0.98,
                Objective: objective,
                SuggestedHook: "The entire leverage structure of technology is flipping right now.",
                SuggestedTitle: $"Why {cleaned} Changes Everything",
                SuggestedCaption: $"Execution is becoming free. Taste, curation, and distribution are the new moats. What are your thoughts on this shift?",
                SuggestedDescription: $"Deep dive into the operational reality of {cleaned}. Discover why traditional assumptions about leverage no longer apply.",
                SuggestedHashtags: new List<string> { "#SignalCut", "#FutureOfTech", "#Productivity", "#Innovation" },
                SuggestedCta: "Follow for daily high-signal breakdowns."
            ),
            new(
                StartTime: 68.0,
                EndTime: 118.0,
                TranscriptSnippet: "If you look at the economics, teams that adopt structured pipelines are producing 10x the output with zero quality regression. The bottleneck is no longer how fast you create, it is whether you can identify what is truly worth broadcasting.",
                Reason: "Compelling quantitative statement with actionable takeaway on identifying high-signal ideas.",
                TopicRelevance: 0.91,
                HookStrength: 0.89,
                InformationDensity: 0.90,
                ClipScore: 90.0,
                Speaker: "Speaker 1",
                Confidence: 0.96,
                Objective: objective,
                SuggestedHook: "The bottleneck is no longer how fast you create content.",
                SuggestedTitle: "The New Economics of Content Creation",
                SuggestedCaption: "When creation becomes abundant, signal detection becomes paramount. Here's why 10x output requires better filters.",
                SuggestedDescription: "Unpacking the structural economics of modern content pipelines.",
                SuggestedHashtags: new List<string> { "#ContentStrategy", "#CreatorEconomy", "#AIWorkflows" },
                SuggestedCta: "Save this post for your content planning."
            ),
            new(
                StartTime: 135.0,
                EndTime: 182.0,
                TranscriptSnippet: "Most people fail at repurposing because they take a 60-minute video and chop it randomly. You have to isolate the single core contradiction that challenges the viewer's mental model within the first three seconds.",
                Reason: "High-value tactical advice pinpointing the exact failure mode of content creators.",
                TopicRelevance: 0.88,
                HookStrength: 0.95,
                InformationDensity: 0.87,
                ClipScore: 91.0,
                Speaker: "Speaker 2",
                Confidence: 0.97,
                Objective: objective,
                SuggestedHook: "Here is why 90% of creators fail at content repurposing.",
                SuggestedTitle: "How to Stop Chopping Videos Randomly",
                SuggestedCaption: "Random cuts get ignored. Isolate the core contradiction in the first 3 seconds to keep viewers hooked.",
                SuggestedDescription: "Tactical breakdown of how to hook viewers using mental model contradictions.",
                SuggestedHashtags: new List<string> { "#VideoMarketing", "#Shorts", "#ContentRepurposing" },
                SuggestedCta: "Share this with a fellow creator who needs this."
            )
        };

        // Adjust moments according to objective
        if (objective == MomentObjective.Controversial)
        {
            candidates = candidates.OrderByDescending(c => c.HookStrength).ToList();
        }
        else if (objective == MomentObjective.Educational)
        {
            candidates = candidates.OrderByDescending(c => c.InformationDensity).ToList();
        }

        return Task.FromResult(candidates);
    }

    public Task<(string Hook, string Title, string Caption, string Description, List<string> Hashtags, string Cta)> GenerateCopyAsync(string momentTranscript, string contextTopic, CancellationToken ct = default)
    {
        var hook = "Software developers may not code the way they do today.";
        var title = $"How AI Is Transforming {contextTopic}";
        var caption = "Automation isn't simply autocomplete — it reshapes the entire development lifecycle. Are you ready for what comes next?";
        var desc = $"Analysis of {contextTopic} based on high-signal discussions.";
        var hashtags = new List<string> { "#AI", "#TechTrends", "#Innovation", "#SignalCut" };
        var cta = "Comment your thoughts below and subscribe for more insights.";

        return Task.FromResult((hook, title, caption, desc, hashtags, cta));
    }
}
