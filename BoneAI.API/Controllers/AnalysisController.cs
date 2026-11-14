using BoneAI.API.Models;
using BoneAI.Application.DTOs;
using BoneAI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoneAI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AnalysisController : ControllerBase
{
    private readonly IAnalysisService _analysisService;
    private readonly IAIModelRepository _modelRepo;
    private readonly IAIServiceClient _aiClient;
    private readonly ILogger<AnalysisController> _logger;

    public AnalysisController(
        IAnalysisService analysisService,
        IAIModelRepository modelRepo,
        IAIServiceClient aiClient,
        ILogger<AnalysisController> logger)
    {
        _analysisService = analysisService;
        _modelRepo = modelRepo;
        _aiClient = aiClient;
        _logger = logger;
    }

    /// <summary>
    /// Submits an X-ray image for asynchronous AI analysis in SingleModel, MultiModel, or CompareAll mode.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AnalysisSubmissionResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Submit(
        [FromBody] SubmitAnalysisRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyHeader,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) && !string.IsNullOrWhiteSpace(idempotencyHeader))
        {
            request.IdempotencyKey = idempotencyHeader.Trim();
        }

        var response = await _analysisService.SubmitAsync(request, cancellationToken);
        return AcceptedAtAction(nameof(GetStatus), new { id = response.AnalysisId }, response);
    }

    /// <summary>
    /// Retrieves current aggregate status, execution runs, and detection results for an analysis.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AnalysisDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(Guid id, CancellationToken cancellationToken)
    {
        var response = await _analysisService.GetStatusAsync(id, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Lists active AI models available for inference (backward compatibility route).
    /// </summary>
    [HttpGet("models")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetModels(CancellationToken cancellationToken)
    {
        var models = await _modelRepo.GetActiveModelsAsync(cancellationToken);
        var result = models.Select(m => new
        {
            m.Id,
            m.ModelKey,
            m.Name,
            m.Version,
            m.Framework,
            m.Description,
            m.IsActive,
            m.IsAvailable
        });
        return Ok(result);
    }

    /// <summary>
    /// Checks Python AI Service health and readiness status.
    /// </summary>
    [HttpGet("ai-service-health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckAIServiceHealth(CancellationToken cancellationToken)
    {
        var isHealthy = await _aiClient.CheckHealthAsync(cancellationToken);
        var isReady = await _aiClient.CheckReadinessAsync(cancellationToken);

        return Ok(new
        {
            liveness = isHealthy ? "Healthy" : "Unhealthy",
            readiness = isReady ? "Ready" : "NotReady",
            checkedAt = DateTime.UtcNow
        });
    }
}
