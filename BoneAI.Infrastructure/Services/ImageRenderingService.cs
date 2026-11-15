using BoneAI.Application.Interfaces;
using BoneAI.Domain.Entities;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BoneAI.Infrastructure.Services;

public class ImageRenderingService : IImageRenderingService
{
    private readonly IAnalysisRunRepository _runRepo;
    private readonly IImageStorageService _storageService;
    private readonly ILogger<ImageRenderingService> _logger;

    private static readonly Rgba32 BoxColor = new(255, 59, 48, 255);       // Vivid medical Red
    private static readonly Rgba32 BannerBgColor = new(20, 20, 20, 220);    // Dark translucent badge
    private static readonly Rgba32 TextColor = new(255, 255, 255, 255);    // White text

    public ImageRenderingService(
        IAnalysisRunRepository runRepo,
        IImageStorageService storageService,
        ILogger<ImageRenderingService> logger)
    {
        _runRepo = runRepo;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<string?> RenderAndSaveRunImageAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await _runRepo.GetByIdAsync(runId, cancellationToken);
        if (run == null)
        {
            _logger.LogWarning("Cannot render result image: AnalysisRun {RunId} not found.", runId);
            return null;
        }

        var xRayImage = run.Analysis?.XRayImage;
        if (xRayImage == null)
        {
            _logger.LogWarning("Cannot render result image: Associated XRayImage for Run {RunId} not found.", runId);
            return null;
        }

        await using var rawStream = await _storageService.GetAsync(xRayImage.FilePath, cancellationToken);
        if (rawStream == null)
        {
            _logger.LogWarning("Cannot render result image: Storage file {Path} not found for Run {RunId}.", xRayImage.FilePath, runId);
            return null;
        }

        try
        {
            using var image = await Image.LoadAsync<Rgba32>(rawStream, cancellationToken);

            // Draw each detection bounding box and label
            foreach (var detection in run.Detections)
            {
                DrawDetection(image, detection);
            }

            using var outputStream = new MemoryStream();
            await image.SaveAsPngAsync(outputStream, cancellationToken);
            outputStream.Position = 0;

            var fileName = $"result_{runId:N}.png";
            var savedPath = await _storageService.SaveAsync(outputStream, fileName, "image/png", cancellationToken);

            run.ResultImagePath = savedPath;
            await _runRepo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Rendered and saved result image for Run {RunId} to {Path}", runId, savedPath);
            return savedPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render result image for Run {RunId}: {Message}", runId, ex.Message);
            return null;
        }
    }

    private static void DrawDetection(Image<Rgba32> image, Detection detection)
    {
        int x = (int)Math.Round(detection.X);
        int y = (int)Math.Round(detection.Y);
        int w = (int)Math.Round(detection.Width);
        int h = (int)Math.Round(detection.Height);

        int imgW = image.Width;
        int imgH = image.Height;

        // Clamp bounding box to image boundaries
        x = Math.Clamp(x, 0, imgW - 1);
        y = Math.Clamp(y, 0, imgH - 1);
        w = Math.Clamp(w, 1, imgW - x);
        h = Math.Clamp(h, 1, imgH - y);

        int thickness = Math.Clamp(Math.Max(imgW, imgH) / 300, 2, 6);

        // 1. Draw Bounding Box rectangle
        DrawRectangle(image, x, y, w, h, thickness, BoxColor);

        // 2. Prepare Label text
        var label = $"{detection.ClassName.ToUpperInvariant()} {(int)Math.Round(detection.Confidence * 100)}%";
        int charW = 6;
        int charH = 8;
        int padding = 4;
        int bannerW = (label.Length * charW) + (padding * 2);
        int bannerH = charH + (padding * 2);

        int bannerX = x;
        int bannerY = y >= bannerH ? y - bannerH : y;
        bannerX = Math.Clamp(bannerX, 0, Math.Max(0, imgW - bannerW));
        bannerY = Math.Clamp(bannerY, 0, Math.Max(0, imgH - bannerH));

        // 3. Draw Banner background
        FillRectangle(image, bannerX, bannerY, Math.Min(bannerW, imgW - bannerX), Math.Min(bannerH, imgH - bannerY), BannerBgColor);

        // 4. Draw Label text pixels
        int textX = bannerX + padding;
        int textY = bannerY + padding;
        DrawString(image, textX, textY, label, TextColor);
    }

    private static void DrawRectangle(Image<Rgba32> image, int x, int y, int w, int h, int thickness, Rgba32 color)
    {
        int xMax = Math.Min(x + w, image.Width);
        int yMax = Math.Min(y + h, image.Height);

        // Top & bottom horizontal bars
        for (int t = 0; t < thickness; t++)
        {
            int topY = y + t;
            int botY = yMax - 1 - t;

            if (topY < image.Height)
            {
                for (int px = x; px < xMax; px++) image[px, topY] = color;
            }
            if (botY >= 0 && botY != topY)
            {
                for (int px = x; px < xMax; px++) image[px, botY] = color;
            }
        }

        // Left & right vertical bars
        for (int t = 0; t < thickness; t++)
        {
            int leftX = x + t;
            int rightX = xMax - 1 - t;

            if (leftX < image.Width)
            {
                for (int py = y; py < yMax; py++) image[leftX, py] = color;
            }
            if (rightX >= 0 && rightX != leftX)
            {
                for (int py = y; py < yMax; py++) image[rightX, py] = color;
            }
        }
    }

    private static void FillRectangle(Image<Rgba32> image, int x, int y, int w, int h, Rgba32 color)
    {
        int xMax = Math.Min(x + w, image.Width);
        int yMax = Math.Min(y + h, image.Height);

        for (int py = y; py < yMax; py++)
        {
            for (int px = x; px < xMax; px++)
            {
                image[px, py] = color;
            }
        }
    }

    private static void DrawString(Image<Rgba32> image, int startX, int startY, string text, Rgba32 color)
    {
        int curX = startX;
        foreach (char c in text)
        {
            DrawChar(image, curX, startY, c, color);
            curX += 6;
        }
    }

    private static void DrawChar(Image<Rgba32> image, int x, int y, char c, Rgba32 color)
    {
        var glyph = GetGlyph(c);
        for (int row = 0; row < 7; row++)
        {
            int py = y + row;
            if (py < 0 || py >= image.Height) continue;

            byte rowBits = glyph[row];
            for (int col = 0; col < 5; col++)
            {
                int px = x + col;
                if (px < 0 || px >= image.Width) continue;

                if ((rowBits & (0x10 >> col)) != 0)
                {
                    image[px, py] = color;
                }
            }
        }
    }

    private static byte[] GetGlyph(char c)
    {
        // 5x7 bitmap font definitions for common ASCII characters
        return c switch
        {
            '0' => new byte[] { 0x0E, 0x11, 0x13, 0x15, 0x19, 0x11, 0x0E },
            '1' => new byte[] { 0x04, 0x0C, 0x04, 0x04, 0x04, 0x04, 0x0E },
            '2' => new byte[] { 0x0E, 0x11, 0x01, 0x06, 0x08, 0x10, 0x1F },
            '3' => new byte[] { 0x1F, 0x02, 0x04, 0x02, 0x01, 0x11, 0x0E },
            '4' => new byte[] { 0x02, 0x06, 0x0A, 0x12, 0x1F, 0x02, 0x02 },
            '5' => new byte[] { 0x1F, 0x10, 0x1E, 0x01, 0x01, 0x11, 0x0E },
            '6' => new byte[] { 0x06, 0x08, 0x10, 0x1E, 0x11, 0x11, 0x0E },
            '7' => new byte[] { 0x1F, 0x01, 0x02, 0x04, 0x08, 0x08, 0x08 },
            '8' => new byte[] { 0x0E, 0x11, 0x11, 0x0E, 0x11, 0x11, 0x0E },
            '9' => new byte[] { 0x0E, 0x11, 0x11, 0x0F, 0x01, 0x02, 0x0C },
            'A' => new byte[] { 0x0E, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 },
            'B' => new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x11, 0x11, 0x1E },
            'C' => new byte[] { 0x0E, 0x11, 0x10, 0x10, 0x10, 0x11, 0x0E },
            'D' => new byte[] { 0x1C, 0x12, 0x11, 0x11, 0x11, 0x12, 0x1C },
            'E' => new byte[] { 0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x1F },
            'F' => new byte[] { 0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x10 },
            'G' => new byte[] { 0x0E, 0x11, 0x10, 0x17, 0x11, 0x11, 0x0F },
            'H' => new byte[] { 0x11, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 },
            'I' => new byte[] { 0x0E, 0x04, 0x04, 0x04, 0x04, 0x04, 0x0E },
            'J' => new byte[] { 0x07, 0x02, 0x02, 0x02, 0x02, 0x12, 0x0C },
            'K' => new byte[] { 0x11, 0x12, 0x14, 0x18, 0x14, 0x12, 0x11 },
            'L' => new byte[] { 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x1F },
            'M' => new byte[] { 0x11, 0x1B, 0x15, 0x15, 0x11, 0x11, 0x11 },
            'N' => new byte[] { 0x11, 0x19, 0x15, 0x13, 0x11, 0x11, 0x11 },
            'O' => new byte[] { 0x0E, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E },
            'P' => new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x10, 0x10, 0x10 },
            'Q' => new byte[] { 0x0E, 0x11, 0x11, 0x11, 0x15, 0x12, 0x0D },
            'R' => new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x14, 0x12, 0x11 },
            'S' => new byte[] { 0x0E, 0x11, 0x10, 0x0E, 0x01, 0x11, 0x0E },
            'T' => new byte[] { 0x1F, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04 },
            'U' => new byte[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E },
            'V' => new byte[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x0A, 0x04 },
            'W' => new byte[] { 0x11, 0x11, 0x11, 0x15, 0x15, 0x15, 0x0A },
            'X' => new byte[] { 0x11, 0x11, 0x0A, 0x04, 0x0A, 0x11, 0x11 },
            'Y' => new byte[] { 0x11, 0x11, 0x0A, 0x04, 0x04, 0x04, 0x04 },
            'Z' => new byte[] { 0x1F, 0x01, 0x02, 0x04, 0x08, 0x10, 0x1F },
            ':' => new byte[] { 0x00, 0x0C, 0x0C, 0x00, 0x0C, 0x0C, 0x00 },
            '.' => new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x0C, 0x0C },
            '%' => new byte[] { 0x19, 0x19, 0x02, 0x04, 0x08, 0x13, 0x13 },
            '-' => new byte[] { 0x00, 0x00, 0x00, 0x1F, 0x00, 0x00, 0x00 },
            '_' => new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1F },
            _ => new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }
        };
    }
}
