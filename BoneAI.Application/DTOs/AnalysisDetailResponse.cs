namespace BoneAI.Application.DTOs;

public class DetectionDto
{
    public Guid Id { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
}

public class AnalysisRunDetailDto
{
    public Guid AnalysisRunId { get; set; }
    public Guid AIModelId { get; set; }
    public string ModelKey { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
    public string Framework { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long? ProcessingTimeMs { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsMockResult { get; set; }
    public string? Warning { get; set; }
    public IReadOnlyList<DetectionDto> Detections { get; set; } = Array.Empty<DetectionDto>();
}

public class AnalysisDetailResponse
{
    public Guid AnalysisId { get; set; }
    public Guid XRayImageId { get; set; }
    public string Mode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int TotalRunsCount { get; set; }
    public int CompletedRunsCount { get; set; }
    public int FailedRunsCount { get; set; }
    public IReadOnlyList<AnalysisRunDetailDto> AnalysisRuns { get; set; } = Array.Empty<AnalysisRunDetailDto>();
}
