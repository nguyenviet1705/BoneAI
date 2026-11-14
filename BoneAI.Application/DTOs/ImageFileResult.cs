namespace BoneAI.Application.DTOs;

public class ImageFileResult
{
    public Stream Stream { get; set; } = Stream.Null;
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
