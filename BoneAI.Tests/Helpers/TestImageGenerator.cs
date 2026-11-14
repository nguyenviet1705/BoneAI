using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BoneAI.Tests.Helpers;

public static class TestImageGenerator
{
    public static byte[] CreateValidJpeg(int width = 10, int height = 10)
    {
        using var image = new Image<Rgb24>(width, height);
        using var ms = new MemoryStream();
        image.SaveAsJpeg(ms);
        return ms.ToArray();
    }

    public static byte[] CreateValidPng(int width = 10, int height = 10)
    {
        using var image = new Image<Rgba32>(width, height);
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }
}
