using Microsoft.Extensions.Logging;
using SignalCut.Application.Interfaces;

namespace SignalCut.Infrastructure.Providers.Storage;

public class LocalStorageProvider : IObjectStorage
{
    private readonly string _basePath;
    private readonly ILogger<LocalStorageProvider> _logger;

    public LocalStorageProvider(ILogger<LocalStorageProvider> logger)
    {
        _logger = logger;
        _basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "storage");
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        var sanitizedKey = key.Replace("/", Path.DirectorySeparatorChar.ToString());
        var fullPath = Path.Combine(_basePath, sanitizedKey);

        var parent = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);

        using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, ct);

        _logger.LogInformation("Stored file at {Path}", fullPath);
        return $"/storage/{key}";
    }

    public Task<Stream> DownloadAsync(string key, CancellationToken ct = default)
    {
        var sanitizedKey = key.Replace("/", Path.DirectorySeparatorChar.ToString());
        var fullPath = Path.Combine(_basePath, sanitizedKey);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("File not found in storage", key);
        }

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task<string> GetSignedUrlAsync(string key, TimeSpan expiresIn)
    {
        // Local simulation returns relative path with expiring token query param
        return Task.FromResult($"/storage/{key}?sig={Guid.NewGuid():N}&exp={DateTimeOffset.UtcNow.Add(expiresIn).ToUnixTimeSeconds()}");
    }

    public Task<bool> DeleteAsync(string key, CancellationToken ct = default)
    {
        var sanitizedKey = key.Replace("/", Path.DirectorySeparatorChar.ToString());
        var fullPath = Path.Combine(_basePath, sanitizedKey);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}
