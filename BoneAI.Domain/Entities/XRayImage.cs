namespace BoneAI.Domain.Entities;

public class XRayImage
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Analysis> Analyses { get; set; } = new List<Analysis>();
}
