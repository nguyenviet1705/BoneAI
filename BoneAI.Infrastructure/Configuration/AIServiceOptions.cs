namespace BoneAI.Infrastructure.Configuration;

public class AIServiceOptions
{
    public const string SectionName = "AIService";

    public string BaseUrl { get; set; } = "http://localhost:8000";
    public int TimeoutSeconds { get; set; } = 60;
    public int HealthCheckTimeoutSeconds { get; set; } = 5;
    public int MaxRetryAttempts { get; set; } = 3;
    public int InitialRetryDelayMs { get; set; } = 1000;
}
