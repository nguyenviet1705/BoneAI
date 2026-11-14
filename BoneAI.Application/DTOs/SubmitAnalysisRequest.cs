using BoneAI.Domain.Enums;

namespace BoneAI.Application.DTOs;

public class SubmitAnalysisRequest
{
    public Guid XRayImageId { get; set; }
    
    /// <summary>
    /// Analysis mode: SingleModel (0), MultiModel (1), or CompareAll (2).
    /// If null, automatically inferred based on provided model IDs.
    /// </summary>
    public AnalysisMode? Mode { get; set; }
    
    /// <summary>
    /// Legacy SingleModel parameter (for backward compatibility with Phase 3).
    /// </summary>
    public Guid? AIModelId { get; set; }

    /// <summary>
    /// List of explicitly selected model IDs for SingleModel or MultiModel.
    /// </summary>
    public List<Guid>? SelectedModelIds { get; set; }

    /// <summary>
    /// Optional idempotency key for preventing duplicate submissions.
    /// </summary>
    public string? IdempotencyKey { get; set; }
}
