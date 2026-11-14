using BoneAI.Domain.Enums;

namespace BoneAI.Domain.Entities;

public class AnalysisRun
{
    public Guid Id { get; set; }
    public Guid AnalysisId { get; set; }
    public Guid AIModelId { get; set; }
    public AnalysisRunStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long? ProcessingTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime? LeaseUntil { get; set; }

    public Analysis? Analysis { get; set; }
    public AIModel? AIModel { get; set; }
    public ICollection<Detection> Detections { get; set; } = new List<Detection>();
}
