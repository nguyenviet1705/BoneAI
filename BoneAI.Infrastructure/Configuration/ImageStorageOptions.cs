namespace BoneAI.Infrastructure.Configuration;

public class ImageStorageOptions
{
    public const string SectionName = "ImageStorage";

    public string RootPath { get; set; } = "Storage/Images";
    public long MaxFileSizeInBytes { get; set; } = 10 * 1024 * 1024; // 10 MB
    public List<string> AllowedExtensions { get; set; } = new() { ".jpg", ".jpeg", ".png" };
    public List<string> AllowedContentTypes { get; set; } = new() { "image/jpeg", "image/png" };
    public int MaxWidth { get; set; } = 8192;
    public int MaxHeight { get; set; } = 8192;
    public long MaxPixelCount { get; set; } = 50_000_000;
}
