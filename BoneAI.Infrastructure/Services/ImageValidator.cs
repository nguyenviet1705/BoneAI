using BoneAI.Application.Interfaces;
using BoneAI.Domain.Exceptions;
using BoneAI.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;

namespace BoneAI.Infrastructure.Services;

public class ImageValidator : IImageValidator
{
    private static readonly byte[] JpegHeader = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly ImageStorageOptions _options;
    private readonly ILogger<ImageValidator> _logger;

    public ImageValidator(IOptions<ImageStorageOptions> options, ILogger<ImageValidator> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ImageValidationResult> ValidateAsync(
        Stream stream,
        string fileName,
        string contentType,
        long? declaredLength = null,
        CancellationToken cancellationToken = default)
    {
        // 1. File size validation
        if (declaredLength.HasValue)
        {
            if (declaredLength.Value <= 0)
            {
                throw new ValidationException("Uploaded file is empty (zero bytes).");
            }

            if (declaredLength.Value > _options.MaxFileSizeInBytes)
            {
                throw new PayloadTooLargeException(
                    $"File size ({declaredLength.Value} bytes) exceeds the maximum allowed limit of {_options.MaxFileSizeInBytes} bytes.");
            }
        }

        if (stream == null || !stream.CanRead)
        {
            throw new ValidationException("File stream cannot be read.");
        }

        if (stream.CanSeek)
        {
            if (stream.Length <= 0)
            {
                throw new ValidationException("Uploaded file is empty (zero bytes).");
            }

            if (stream.Length > _options.MaxFileSizeInBytes)
            {
                throw new PayloadTooLargeException(
                    $"File stream length ({stream.Length} bytes) exceeds the maximum allowed limit of {_options.MaxFileSizeInBytes} bytes.");
            }

            stream.Position = 0;
        }

        // 2. Extension validation
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) || !_options.AllowedExtensions.Contains(extension))
        {
            throw new UnsupportedMediaTypeException(
                $"File extension '{extension}' is not supported. Allowed extensions: {string.Join(", ", _options.AllowedExtensions)}.");
        }

        // 3. MIME type validation
        var normalizedMime = contentType?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!_options.AllowedContentTypes.Contains(normalizedMime))
        {
            throw new UnsupportedMediaTypeException(
                $"Content-Type '{contentType}' is not supported. Allowed MIME types: {string.Join(", ", _options.AllowedContentTypes)}.");
        }

        // 4. File signature (magic bytes) validation
        var header = new byte[8];
        var bytesRead = await stream.ReadAsync(header.AsMemory(0, 8), cancellationToken);
        if (bytesRead < 3)
        {
            throw new ValidationException("Uploaded file is too small to contain a valid image header.");
        }

        var isJpegMagic = header[0] == JpegHeader[0] && header[1] == JpegHeader[1] && header[2] == JpegHeader[2];
        var isPngMagic = bytesRead >= 8 &&
                         header[0] == PngHeader[0] && header[1] == PngHeader[1] &&
                         header[2] == PngHeader[2] && header[3] == PngHeader[3] &&
                         header[4] == PngHeader[4] && header[5] == PngHeader[5] &&
                         header[6] == PngHeader[6] && header[7] == PngHeader[7];

        if (!isJpegMagic && !isPngMagic)
        {
            throw new UnsupportedMediaTypeException("File signature (magic bytes) does not match supported image formats (JPEG or PNG).");
        }

        // Validate extension matches detected signature
        var isJpegExtension = extension is ".jpg" or ".jpeg";
        var isPngExtension = extension is ".png";

        if (isJpegMagic && !isJpegExtension)
        {
            throw new ValidationException($"Mismatched file format: file signature indicates JPEG but extension is '{extension}'.");
        }

        if (isPngMagic && !isPngExtension)
        {
            throw new ValidationException($"Mismatched file format: file signature indicates PNG but extension is '{extension}'.");
        }

        // Reset stream for decoding
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        // 5. Actual image decoding using ImageSharp
        int width;
        int height;
        string detectedFormat;
        string detectedMimeType;

        try
        {
            var imageInfo = await Image.IdentifyAsync(stream, cancellationToken);
            if (imageInfo == null)
            {
                throw new ValidationException("Unable to identify image header. File may be corrupted or disguised.");
            }

            detectedFormat = imageInfo.Metadata.DecodedImageFormat?.Name ?? string.Empty;
            detectedMimeType = imageInfo.Metadata.DecodedImageFormat?.DefaultMimeType ?? string.Empty;
            width = imageInfo.Width;
            height = imageInfo.Height;

            // Rewind stream and attempt full load to ensure pixels are not corrupted/truncated
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            using var loadedImage = await Image.LoadAsync(stream, cancellationToken);
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Image decoding failed for file {FileName}: {Message}", fileName, ex.Message);
            throw new ValidationException($"Image decoding failed: file data is corrupt or not a valid image. Details: {ex.Message}");
        }
        finally
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }
        }

        // Verify format matches extension
        if (isJpegExtension && !detectedFormat.Equals("JPEG", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException($"File content format ({detectedFormat}) does not match JPEG extension.");
        }

        if (isPngExtension && !detectedFormat.Equals("PNG", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException($"File content format ({detectedFormat}) does not match PNG extension.");
        }

        // 6. Dimensions and pixel count validation
        if (width <= 0 || height <= 0)
        {
            throw new ValidationException($"Invalid image dimensions: {width}x{height}.");
        }

        if (width > _options.MaxWidth || height > _options.MaxHeight)
        {
            throw new ValidationException(
                $"Image dimensions ({width}x{height}) exceed maximum allowed dimensions ({_options.MaxWidth}x{_options.MaxHeight}).");
        }

        var totalPixels = (long)width * height;
        if (totalPixels > _options.MaxPixelCount)
        {
            throw new ValidationException(
                $"Total pixel count ({totalPixels}) exceeds maximum allowed ({_options.MaxPixelCount}). Potential decompression bomb prevented.");
        }

        var fileSize = stream.CanSeek ? stream.Length : (declaredLength ?? 0);

        return new ImageValidationResult(
            IsValid: true,
            DetectedFormat: detectedFormat,
            DetectedMimeType: detectedMimeType,
            Width: width,
            Height: height,
            Size: fileSize);
    }
}
