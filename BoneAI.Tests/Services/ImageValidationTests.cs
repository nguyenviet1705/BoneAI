using System.Text;
using BoneAI.Domain.Exceptions;
using BoneAI.Infrastructure.Configuration;
using BoneAI.Infrastructure.Services;
using BoneAI.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BoneAI.Tests.Services;

public class ImageValidationTests
{
    private readonly ImageValidator _validator;
    private readonly ImageStorageOptions _options;

    public ImageValidationTests()
    {
        _options = new ImageStorageOptions
        {
            MaxFileSizeInBytes = 10 * 1024 * 1024, // 10 MB
            AllowedExtensions = new List<string> { ".jpg", ".jpeg", ".png" },
            AllowedContentTypes = new List<string> { "image/jpeg", "image/png" },
            MaxWidth = 4000,
            MaxHeight = 4000,
            MaxPixelCount = 16_000_000
        };

        _validator = new ImageValidator(
            Options.Create(_options),
            NullLogger<ImageValidator>.Instance);
    }

    [Fact]
    public async Task ValidateAsync_ValidJpeg_PassesValidation()
    {
        var bytes = TestImageGenerator.CreateValidJpeg(100, 100);
        using var stream = new MemoryStream(bytes);

        var result = await _validator.ValidateAsync(stream, "test.jpg", "image/jpeg", bytes.Length);

        Assert.True(result.IsValid);
        Assert.Equal("JPEG", result.DetectedFormat);
        Assert.Equal("image/jpeg", result.DetectedMimeType);
        Assert.Equal(100, result.Width);
        Assert.Equal(100, result.Height);
    }

    [Fact]
    public async Task ValidateAsync_ValidPng_PassesValidation()
    {
        var bytes = TestImageGenerator.CreateValidPng(80, 80);
        using var stream = new MemoryStream(bytes);

        var result = await _validator.ValidateAsync(stream, "wrist.png", "image/png", bytes.Length);

        Assert.True(result.IsValid);
        Assert.Equal("PNG", result.DetectedFormat);
        Assert.Equal("image/png", result.DetectedMimeType);
        Assert.Equal(80, result.Width);
        Assert.Equal(80, result.Height);
    }

    [Fact]
    public async Task ValidateAsync_UnsupportedExtension_ThrowsUnsupportedMediaTypeException()
    {
        var bytes = TestImageGenerator.CreateValidJpeg(50, 50);
        using var stream = new MemoryStream(bytes);

        await Assert.ThrowsAsync<UnsupportedMediaTypeException>(() =>
            _validator.ValidateAsync(stream, "image.gif", "image/gif", bytes.Length));
    }

    [Fact]
    public async Task ValidateAsync_EmptyFile_ThrowsValidationException()
    {
        using var stream = new MemoryStream(Array.Empty<byte>());

        await Assert.ThrowsAsync<ValidationException>(() =>
            _validator.ValidateAsync(stream, "empty.jpg", "image/jpeg", 0));
    }

    [Fact]
    public async Task ValidateAsync_FileExceedingMaxSize_ThrowsPayloadTooLargeException()
    {
        _options.MaxFileSizeInBytes = 500; // Small limit for test
        var bytes = TestImageGenerator.CreateValidJpeg(100, 100);
        using var stream = new MemoryStream(bytes);

        await Assert.ThrowsAsync<PayloadTooLargeException>(() =>
            _validator.ValidateAsync(stream, "large.jpg", "image/jpeg", bytes.Length));
    }

    [Fact]
    public async Task ValidateAsync_InvalidMagicBytes_ThrowsUnsupportedMediaTypeException()
    {
        // Text file disguised as JPEG
        var bytes = Encoding.UTF8.GetBytes("This is not a real JPEG image file");
        using var stream = new MemoryStream(bytes);

        await Assert.ThrowsAsync<UnsupportedMediaTypeException>(() =>
            _validator.ValidateAsync(stream, "fake.jpg", "image/jpeg", bytes.Length));
    }

    [Fact]
    public async Task ValidateAsync_MismatchedExtensionAndSignature_ThrowsValidationException()
    {
        // Real JPEG bytes with .png extension
        var bytes = TestImageGenerator.CreateValidJpeg(50, 50);
        using var stream = new MemoryStream(bytes);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _validator.ValidateAsync(stream, "mismatched.png", "image/png", bytes.Length));
    }

    [Fact]
    public async Task ValidateAsync_CorruptedImageData_ThrowsValidationException()
    {
        // Real JPEG magic bytes followed by garbage
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 };
        using var stream = new MemoryStream(bytes);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _validator.ValidateAsync(stream, "corrupt.jpg", "image/jpeg", bytes.Length));
    }

    [Fact]
    public async Task ValidateAsync_DimensionsExceedingLimit_ThrowsValidationException()
    {
        _options.MaxWidth = 50;
        _options.MaxHeight = 50;
        var bytes = TestImageGenerator.CreateValidJpeg(100, 100);
        using var stream = new MemoryStream(bytes);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _validator.ValidateAsync(stream, "huge_dim.jpg", "image/jpeg", bytes.Length));
    }

    [Fact]
    public async Task ValidateAsync_PixelCountExceedingLimit_ThrowsValidationException()
    {
        _options.MaxPixelCount = 500; // 500 pixels max
        var bytes = TestImageGenerator.CreateValidJpeg(50, 50); // 2500 pixels
        using var stream = new MemoryStream(bytes);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _validator.ValidateAsync(stream, "bomb.jpg", "image/jpeg", bytes.Length));
    }
}
