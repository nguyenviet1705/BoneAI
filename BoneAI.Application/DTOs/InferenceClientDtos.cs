namespace BoneAI.Application.DTOs;

public class InferenceClientRequest
{
    public Guid AnalysisRunId { get; set; }
    public string ModelKey { get; set; } = string.Empty;
    public string? ModelVersion { get; set; }
    public Stream ImageStream { get; set; } = Stream.Null;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}

public class InferenceDetectionItem
{
    public string ClassName { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
}

public class InferenceClientResponse
{
    public Guid AnalysisRunId { get; set; }
    public string ModelKey { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public long ProcessingTimeMs { get; set; }
    public List<InferenceDetectionItem> Detections { get; set; } = new();
    public bool IsMockResult { get; set; }
    public string? Warning { get; set; }
}
