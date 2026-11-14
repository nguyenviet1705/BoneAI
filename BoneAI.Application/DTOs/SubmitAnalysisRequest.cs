namespace BoneAI.Application.DTOs;

public class SubmitAnalysisRequest
{
    public Guid XRayImageId { get; set; }
    public Guid AIModelId { get; set; }
    public string? IdempotencyKey { get; set; }
}
