namespace BoneAI.Domain.Entities;

public class AIModel
{
    public Guid Id { get; set; }
    public string ModelKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Framework { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ArtifactPath { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsAvailable { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AnalysisRun> AnalysisRuns { get; set; } = new List<AnalysisRun>();
}
