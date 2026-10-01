using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using SignalCut.Application.Common;
using SignalCut.Application.Interfaces;

namespace SignalCut.Infrastructure.Security;

public class MediaSecurityValidator : IMediaProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MediaSecurityValidator> _logger;
    private const long MaxFileSizeBytes = 500 * 1024 * 1024; // 500 MB max

    public MediaSecurityValidator(HttpClient httpClient, ILogger<MediaSecurityValidator> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> ValidateUrlSafetyAsync(string mediaUrl)
    {
        if (string.IsNullOrWhiteSpace(mediaUrl))
        {
            throw new SecurityException("Media URL cannot be empty.");
        }

        if (!Uri.TryCreate(mediaUrl, UriKind.Absolute, out var uri))
        {
            throw new SecurityException("Invalid URL format.");
        }

        // 1. Validate protocol: HTTP/HTTPS only
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new SecurityException($"Unauthorized URL protocol '{uri.Scheme}'. Only HTTP and HTTPS are permitted.");
        }

        // 2. Prevent SSRF: Resolve IP addresses and inspect for private/loopback/cloud metadata
        try
        {
            var hostAddresses = await Dns.GetHostAddressesAsync(uri.Host);
            if (hostAddresses.Length == 0)
            {
                throw new SecurityException($"Unable to resolve host: {uri.Host}");
            }

            foreach (var ip in hostAddresses)
            {
                if (IsPrivateOrRestrictedIp(ip))
                {
                    _logger.LogWarning("SSRF attempt detected and blocked for host {Host} resolving to restricted IP {IP}", uri.Host, ip);
                    throw new SecurityException($"Access to internal/private IP '{ip}' is blocked for security.");
                }
            }
        }
        catch (SecurityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DNS resolution failed for {Host}", uri.Host);
            throw new SecurityException($"Host validation failed for '{uri.Host}': {ex.Message}");
        }

        return true;
    }

    public async Task<Stream> DownloadMediaStreamAsync(string mediaUrl, CancellationToken ct = default)
    {
        await ValidateUrlSafetyAsync(mediaUrl);

        using var request = new HttpRequestMessage(HttpMethod.Get, mediaUrl);
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength.HasValue && response.Content.Headers.ContentLength.Value > MaxFileSizeBytes)
        {
            throw new SecurityException($"Media file size {response.Content.Headers.ContentLength.Value} bytes exceeds maximum limit of {MaxFileSizeBytes} bytes.");
        }

        var memoryStream = new MemoryStream();
        await response.Content.CopyToAsync(memoryStream, ct);
        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task<Stream> ExtractAudioAsync(string mediaUrl, CancellationToken ct = default)
    {
        // Extracts or provides audio stream
        return await DownloadMediaStreamAsync(mediaUrl, ct);
    }

    private static bool IsPrivateOrRestrictedIp(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip)) return true; // 127.0.0.0/8 or ::1

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast;
        }

        var bytes = ip.GetAddressBytes();

        // 10.0.0.0/8
        if (bytes[0] == 10) return true;

        // 172.16.0.0/12
        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;

        // 192.168.0.0/16
        if (bytes[0] == 192 && bytes[1] == 168) return true;

        // 169.254.0.0/16 (Link Local & AWS/GCP/Azure Cloud Metadata 169.254.169.254)
        if (bytes[0] == 169 && bytes[1] == 254) return true;

        // 0.0.0.0/8
        if (bytes[0] == 0) return true;

        return false;
    }
}
