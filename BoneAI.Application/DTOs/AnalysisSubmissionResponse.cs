namespace BoneAI.Application.DTOs;

public class AnalysisSubmissionResponse
{
    public Guid AnalysisId { get; set; }
    public Guid AnalysisRunId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
