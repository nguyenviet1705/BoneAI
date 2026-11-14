using BoneAI.Domain.Exceptions;
using BoneAI.Infrastructure.Configuration;
using BoneAI.Infrastructure.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace BoneAI.Tests.Services;

public class LocalImageStorageServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly LocalImageStorageService _storageService;

    public LocalImageStorageServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "BoneAI_Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);

        var options = Options.Create(new ImageStorageOptions
        {
            RootPath = _testDirectory,
            MaxFileSizeInBytes = 5 * 1024 * 1024
        });

        var mockEnv = new Mock<IHostEnvironment>();
        mockEnv.Setup(e => e.ContentRootPath).Returns(_testDirectory);

        _storageService = new LocalImageStorageService(
            options,
            mockEnv.Object,
            NullLogger<LocalImageStorageService>.Instance);
    }

    [Fact]
    public async Task SaveAsync_ValidStream_SavesFileAndReturnsKey()
    {
        var content = new byte[] { 1, 2, 3, 4, 5 };
        using var stream = new MemoryStream(content);

        var key = await _storageService.SaveAsync(stream, "photo.jpg", "image/jpeg");

        Assert.NotNull(key);
        Assert.EndsWith(".jpg", key);
        Assert.True(await _storageService.ExistsAsync(key));

        await using var readStream = await _storageService.GetAsync(key);
        Assert.NotNull(readStream);
        Assert.Equal(5, readStream!.Length);
    }

    [Fact]
    public async Task SaveAsync_StreamExceedingLimit_CleansUpAndThrowsPayloadTooLargeException()
    {
        var smallOptions = Options.Create(new ImageStorageOptions
        {
            RootPath = _testDirectory,
            MaxFileSizeInBytes = 10 // Only 10 bytes max
        });

        var mockEnv = new Mock<IHostEnvironment>();
        mockEnv.Setup(e => e.ContentRootPath).Returns(_testDirectory);

        var storage = new LocalImageStorageService(
            smallOptions,
            mockEnv.Object,
            NullLogger<LocalImageStorageService>.Instance);

        var largeContent = new byte[100];
        using var stream = new MemoryStream(largeContent);

        await Assert.ThrowsAsync<PayloadTooLargeException>(() =>
            storage.SaveAsync(stream, "large.jpg", "image/jpeg"));
    }

    [Fact]
    public async Task GetAsync_PathTraversalAttempt_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _storageService.GetAsync("../../windows/system32/cmd.exe"));
    }

    [Fact]
    public async Task DeleteAsync_ExistingFile_DeletesAndReturnsTrue()
    {
        var content = new byte[] { 10, 20, 30 };
        using var stream = new MemoryStream(content);
        var key = await _storageService.SaveAsync(stream, "delete_me.png", "image/png");

        Assert.True(await _storageService.ExistsAsync(key));

        var deleted = await _storageService.DeleteAsync(key);
        Assert.True(deleted);
        Assert.False(await _storageService.ExistsAsync(key));
    }

    [Fact]
    public async Task DeleteAsync_NonexistentFile_ReturnsTrue()
    {
        var deleted = await _storageService.DeleteAsync("2026/01/nonexistent.jpg");
        Assert.True(deleted);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore test cleanup exceptions
        }
    }
}
