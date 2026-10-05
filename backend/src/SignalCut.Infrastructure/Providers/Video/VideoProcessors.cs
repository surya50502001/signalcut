using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SignalCut.Application.Interfaces;

namespace SignalCut.Infrastructure.Providers.Video;

public class CompositeVideoProcessor : IVideoProcessor
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<CompositeVideoProcessor> _logger;

    public CompositeVideoProcessor(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<CompositeVideoProcessor> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<RenderVideoResult> RenderClipAsync(RenderVideoRequest request, Action<int>? onProgress = null, CancellationToken ct = default)
    {
        var workerUrl = _config["AI_WORKER_URL"] ?? "http://localhost:8000";
        _logger.LogInformation("Initiating video render for clip {ClipId} via AI Worker at {Url}", request.ClipId, workerUrl);

        onProgress?.Invoke(20);

        try
        {
            using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"{workerUrl}/api/v1/render")
            {
                Content = JsonContent.Create(request)
            };
            var apiKey = _config["AI_WORKER_API_KEY"];
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                reqMsg.Headers.Add("X-API-Key", apiKey);
            }

            var response = await _httpClient.SendAsync(reqMsg, ct);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<RenderVideoResult>(cancellationToken: ct);
                if (result != null && result.Success)
                {
                    onProgress?.Invoke(100);
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FastAPI AI worker render request failed or unreachable; running local FFmpeg rendering.");
        }

        // Direct local rendering fallback
        onProgress?.Invoke(50);
        return await RenderLocalFallbackAsync(request, onProgress, ct);
    }

    private async Task<RenderVideoResult> RenderLocalFallbackAsync(RenderVideoRequest request, Action<int>? onProgress, CancellationToken ct)
    {
        var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "renders");
        Directory.CreateDirectory(outputDir);

        var outputFileName = $"clip_{request.ClipId}_{DateTime.UtcNow.Ticks}.mp4";
        var outputPath = Path.Combine(outputDir, outputFileName);
        var duration = Math.Max(1.0, request.EndTime - request.StartTime);

        onProgress?.Invoke(70);

        if (!File.Exists(request.SourceVideoUrl))
        {
            return new RenderVideoResult(
                Success: false,
                StorageKey: "",
                StorageUrl: "",
                ThumbnailUrl: null,
                DurationSeconds: 0,
                ErrorMessage: $"Local media source not found at '{request.SourceVideoUrl}'. Real media acquisition is required."
            );
        }

        // Check if FFmpeg is executable on host
        bool ffmpegSuccess = false;
        try
        {
            var watermarkText = request.HasWatermark ? "drawtext=text='SignalCut Free':fontcolor=white@0.7:fontsize=36:x=w-tw-40:y=40," : "";
            var escapedHook = request.Captions.FirstOrDefault()?.Text?.Replace("'", "\\'") ?? "SignalCut High-Signal Clip";
            if (escapedHook.Length > 40) escapedHook = escapedHook[..37] + "...";

            var ffmpegArgs = $"-y -ss {request.StartTime} -to {request.EndTime} -i \"{request.SourceVideoUrl}\" " +
                             $"-vf \"scale=1080:1920:force_original_aspect_ratio=decrease,pad=1080:1920:(ow-iw)/2:(oh-ih)/2," +
                             $"{watermarkText}drawbox=y=ih-120:color={request.HighlightColorHex}@1:width=iw:height=16:t=fill," +
                             $"drawtext=text='{escapedHook}':fontcolor={request.PrimaryColorHex}:fontsize=48:x=(w-tw)/2:y=(h-th)/2\" " +
                             $"-t {Math.Min(60, Math.Ceiling(duration))} -c:v libx264 -pix_fmt yuv420p -c:a aac \"{outputPath}\"";

            var psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = ffmpegArgs,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                var stderr = await process.StandardError.ReadToEndAsync(ct);
                await process.WaitForExitAsync(ct);
                if (process.ExitCode == 0 && File.Exists(outputPath))
                {
                    ffmpegSuccess = true;
                    _logger.LogInformation("Successfully rendered 9:16 video using host FFmpeg at {Path}", outputPath);
                }
                else
                {
                    _logger.LogWarning("FFmpeg exited with code {Code}: {Err}", process.ExitCode, stderr);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Direct FFmpeg execution failed.");
        }

        if (!ffmpegSuccess)
        {
            return new RenderVideoResult(
                Success: false,
                StorageKey: "",
                StorageUrl: "",
                ThumbnailUrl: null,
                DurationSeconds: 0,
                ErrorMessage: "Video rendering engine failed. Ensure ai-worker is running or FFmpeg is installed on the host."
            );
        }

        onProgress?.Invoke(100);

        var cdnUrl = $"/renders/{outputFileName}";
        return new RenderVideoResult(
            Success: true,
            StorageKey: $"renders/{outputFileName}",
            StorageUrl: cdnUrl,
            ThumbnailUrl: null,
            DurationSeconds: duration,
            ErrorMessage: null
        );
    }
}
