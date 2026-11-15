using BoneAI.API.Models;
using BoneAI.Application.DTOs;
using BoneAI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoneAI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ResultsController : ControllerBase
{
    private readonly IResultsService _resultsService;
    private readonly ILogger<ResultsController> _logger;

    public ResultsController(IResultsService resultsService, ILogger<ResultsController> logger)
    {
        _resultsService = resultsService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves comprehensive analysis results, including original image metadata, overall status, and all model runs.
    /// </summary>
    [HttpGet("{analysisId:guid}")]
    [ProducesResponseType(typeof(AnalysisResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnalysisResult(Guid analysisId, CancellationToken cancellationToken)
    {
        var result = await _resultsService.GetAnalysisResultAsync(analysisId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves the execution result, model details, and isolated detections for a single analysis run.
    /// </summary>
    [HttpGet("runs/{runId:guid}")]
    [ProducesResponseType(typeof(AnalysisRunResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnalysisRunResult(Guid runId, CancellationToken cancellationToken)
    {
        var result = await _resultsService.GetAnalysisRunResultAsync(runId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves standardized comparison data across all models evaluated on an X-ray image.
    /// </summary>
    [HttpGet("{analysisId:guid}/comparison")]
    [ProducesResponseType(typeof(AnalysisComparisonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnalysisComparison(Guid analysisId, CancellationToken cancellationToken)
    {
        var result = await _resultsService.GetAnalysisComparisonAsync(analysisId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves the rendered result image containing visual bounding boxes and detection tags for an analysis run.
    /// </summary>
    [HttpGet("runs/{runId:guid}/image")]
    [Produces("image/png", "image/jpeg")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunResultImage(Guid runId, CancellationToken cancellationToken)
    {
        var result = await _resultsService.GetRunResultImageAsync(runId, cancellationToken);
        return File(result.Stream, result.ContentType, enableRangeProcessing: true);
    }
}
