using BoneAI.Application.Interfaces;
using BoneAI.Domain.Exceptions;
using BoneAI.Infrastructure.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BoneAI.Infrastructure.Services;

public class LocalImageStorageService : IImageStorageService
{
    private readonly string _storageRoot;
    private readonly ImageStorageOptions _options;
    private readonly ILogger<LocalImageStorageService> _logger;

    public LocalImageStorageService(
        IOptions<ImageStorageOptions> options,
        IHostEnvironment hostEnvironment,
        ILogger<LocalImageStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var configuredPath = _options.RootPath;
        _storageRoot = Path.IsPathRooted(configuredPath)
            ? Path.GetFullPath(configuredPath)
            : Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, configuredPath));

        if (!Directory.Exists(_storageRoot))
        {
            Directory.CreateDirectory(_storageRoot);
            _logger.LogInformation("Created image storage root directory at {StorageRoot}", _storageRoot);
        }
    }

    public async Task<string> SaveAsync(
        Stream stream,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var rawExtension = Path.GetExtension(originalFileName)?.ToLowerInvariant();
        var safeExtension = rawExtension is ".png" ? ".png" : ".jpg";

        var now = DateTime.UtcNow;
        var subDir = Path.Combine(now.ToString("yyyy"), now.ToString("MM"));
        var uniqueFileName = $"{Guid.NewGuid():N}{safeExtension}";
        var relativeKey = Path.Combine(subDir, uniqueFileName).Replace('\\', '/');

        var physicalPath = ResolveAndValidatePath(relativeKey);
        var parentDir = Path.GetDirectoryName(physicalPath);
        if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
        {
            Directory.CreateDirectory(parentDir);
        }

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        // Stream to disk and enforce max size limit
        long totalBytesWritten = 0;
        var buffer = new byte[81920]; // 80 KB buffer

        try
        {
            await using var destinationStream = new FileStream(
                physicalPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                totalBytesWritten += bytesRead;
                if (totalBytesWritten > _options.MaxFileSizeInBytes)
                {
                    throw new PayloadTooLargeException(
                        $"File size exceeded the maximum allowed limit of {_options.MaxFileSizeInBytes} bytes during storage streaming.");
                }

                await destinationStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            }

            await destinationStream.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write file stream to {PhysicalPath}. Cleaning up partially written file.", physicalPath);
            if (File.Exists(physicalPath))
            {
                try
                {
                    File.Delete(physicalPath);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(cleanupEx, "Failed to clean up partial file at {PhysicalPath}", physicalPath);
                }
            }

            throw;
        }

        _logger.LogInformation("Successfully stored image file at key {RelativeKey} ({Bytes} bytes)", relativeKey, totalBytesWritten);
        return relativeKey;
    }

    public Task<Stream?> GetAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var physicalPath = ResolveAndValidatePath(storageKey);
        if (!File.Exists(physicalPath))
        {
            _logger.LogWarning("File not found on disk for storage key {StorageKey} at path {PhysicalPath}", storageKey, physicalPath);
            return Task.FromResult<Stream?>(null);
        }

        var stream = new FileStream(
            physicalPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var physicalPath = ResolveAndValidatePath(storageKey);
        if (!File.Exists(physicalPath))
        {
            _logger.LogWarning("Attempted to delete physical file {StorageKey}, but it does not exist at {PhysicalPath}", storageKey, physicalPath);
            return Task.FromResult(true); // File already absent
        }

        try
        {
            File.Delete(physicalPath);
            _logger.LogInformation("Deleted physical file {PhysicalPath} for key {StorageKey}", physicalPath, storageKey);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete physical file at {PhysicalPath} for key {StorageKey}", physicalPath, storageKey);
            return Task.FromResult(false);
        }
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var physicalPath = ResolveAndValidatePath(storageKey);
        return Task.FromResult(File.Exists(physicalPath));
    }

    private string ResolveAndValidatePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ValidationException("Storage key cannot be null or empty.");
        }

        // Prevent path traversal
        var normalizedKey = storageKey.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var combined = Path.Combine(_storageRoot, normalizedKey);
        var fullPath = Path.GetFullPath(combined);

        if (!fullPath.StartsWith(_storageRoot, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Path traversal attempt detected: {StorageKey}", storageKey);
            throw new ValidationException("Invalid storage key path traversal.");
        }

        return fullPath;
    }
}
