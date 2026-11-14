namespace BoneAI.Application.DTOs;

public class ImageResponse
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }
    public string FileUrl { get; set; } = string.Empty;
}
