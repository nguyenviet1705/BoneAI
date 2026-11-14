using BoneAI.Domain.Enums;

namespace BoneAI.Domain.Entities;

public class Analysis
{
    public Guid Id { get; set; }
    public Guid XRayImageId { get; set; }
    public AnalysisStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? IdempotencyKey { get; set; }

    public XRayImage? XRayImage { get; set; }
    public ICollection<AnalysisRun> AnalysisRuns { get; set; } = new List<AnalysisRun>();
}
