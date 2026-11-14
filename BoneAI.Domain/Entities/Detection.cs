namespace BoneAI.Domain.Entities;

public class Detection
{
    public Guid Id { get; set; }
    public Guid AnalysisRunId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }

    public AnalysisRun? AnalysisRun { get; set; }
}
