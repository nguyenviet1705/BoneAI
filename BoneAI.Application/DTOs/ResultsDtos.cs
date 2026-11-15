namespace BoneAI.Application.DTOs;

public class BoundingBoxDto
{
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
}

public class DetectionItemDto
{
    public Guid DetectionId { get; set; }
    public string ClassLabel { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; }
    public BoundingBoxDto BoundingBox { get; set; } = new();
}

public class ModelInfoDto
{
    public Guid ModelId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string ModelKey { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Framework { get; set; } = string.Empty;
}

public class OriginalImageInfoDto
{
    public Guid ImageId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string OriginalImageUrl { get; set; } = string.Empty;
}

public class AnalysisRunResultDto
{
    public Guid RunId { get; set; }
    public Guid AnalysisId { get; set; }
    public ModelInfoDto Model { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public double? ProcessingTime { get; set; }
    public long? ProcessingTimeMs { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ResultImageUrl { get; set; }
    public int DetectionCount { get; set; }
    public bool IsMockResult { get; set; }
    public string? Warning { get; set; }
    public IReadOnlyList<DetectionItemDto> Detections { get; set; } = Array.Empty<DetectionItemDto>();
}

public class AnalysisResultDto
{
    public Guid AnalysisId { get; set; }
    public Guid ImageId { get; set; }
    public OriginalImageInfoDto OriginalImage { get; set; } = new();
    public string OverallStatus { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int TotalRunsCount { get; set; }
    public int CompletedRunsCount { get; set; }
    public int FailedRunsCount { get; set; }
    public IReadOnlyList<AnalysisRunResultDto> Runs { get; set; } = Array.Empty<AnalysisRunResultDto>();
}

public class ModelComparisonItemDto
{
    public Guid RunId { get; set; }
    public Guid ModelId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string ModelKey { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double? ProcessingTime { get; set; }
    public int DetectionCount { get; set; }
    public decimal? AverageConfidence { get; set; }
    public IReadOnlyList<DetectionItemDto> Detections { get; set; } = Array.Empty<DetectionItemDto>();
    public string? ResultImageUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsMockResult { get; set; }
    public string? Warning { get; set; }
}

public class AnalysisComparisonDto
{
    public Guid AnalysisId { get; set; }
    public Guid ImageId { get; set; }
    public string OverallStatus { get; set; } = string.Empty;
    public string OriginalImageUrl { get; set; } = string.Empty;
    public IReadOnlyList<ModelComparisonItemDto> Models { get; set; } = Array.Empty<ModelComparisonItemDto>();
}
