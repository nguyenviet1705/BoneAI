namespace BoneAI.Application.DTOs;

public class AIServiceModelDto
{
    public string ModelKey { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Framework { get; set; } = string.Empty;
    public string? ArtifactPath { get; set; }
    public string? AdapterId { get; set; }
    public bool IsActive { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> SupportedInputFormats { get; set; } = new();
    public string? Description { get; set; }
    public Dictionary<string, string>? ClassMapping { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AIServiceModelSyncResultDto
{
    public int DiscoveredCount { get; set; }
    public int LoadedCount { get; set; }
    public int FailedCount { get; set; }
    public List<AIServiceModelDto> Models { get; set; } = new();
}

public class AIModelResponse
{
    public Guid Id { get; set; }
    public string ModelKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Framework { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AIModelStatusResponse
{
    public Guid Id { get; set; }
    public string ModelKey { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsAvailable { get; set; }
    public string? AiServiceStatus { get; set; }
    public string? AiServiceErrorMessage { get; set; }
    public DateTime CheckedAt { get; set; }
}

public class SyncModelsResponse
{
    public int DiscoveredInAiService { get; set; }
    public int CreatedInDatabase { get; set; }
    public int UpdatedInDatabase { get; set; }
    public int UnchangedInDatabase { get; set; }
    public IReadOnlyList<AIModelResponse> Models { get; set; } = Array.Empty<AIModelResponse>();
}

public class UpdateModelStatusRequest
{
    public bool IsActive { get; set; }
}
