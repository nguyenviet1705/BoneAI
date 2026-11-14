using BoneAI.API.Models;
using BoneAI.Application.DTOs;
using BoneAI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoneAI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ModelsController : ControllerBase
{
    private readonly IModelManagementService _modelService;
    private readonly ILogger<ModelsController> _logger;

    public ModelsController(
        IModelManagementService modelService,
        ILogger<ModelsController> logger)
    {
        _modelService = modelService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all registered AI models.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AIModelResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetModels([FromQuery] bool? activeOnly, CancellationToken cancellationToken)
    {
        var models = await _modelService.GetModelsAsync(activeOnly, cancellationToken);
        return Ok(models);
    }

    /// <summary>
    /// Retrieves metadata for a specific AI model by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AIModelResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetModelById(Guid id, CancellationToken cancellationToken)
    {
        var model = await _modelService.GetModelByIdAsync(id, cancellationToken);
        if (model == null)
        {
            return NotFound(new ErrorResponse
            {
                StatusCode = StatusCodes.Status404NotFound,
                Message = $"AI Model with ID '{id}' was not found.",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return Ok(model);
    }

    /// <summary>
    /// Retrieves live availability and inference service loading status for a model.
    /// </summary>
    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(typeof(AIModelStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetModelStatus(Guid id, CancellationToken cancellationToken)
    {
        var status = await _modelService.GetModelStatusAsync(id, cancellationToken);
        if (status == null)
        {
            return NotFound(new ErrorResponse
            {
                StatusCode = StatusCodes.Status404NotFound,
                Message = $"AI Model with ID '{id}' was not found.",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return Ok(status);
    }

    /// <summary>
    /// Synchronizes the backend model catalog with the Python AI Service.
    /// </summary>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(SyncModelsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SyncModels(CancellationToken cancellationToken)
    {
        var result = await _modelService.SyncModelsWithAIServiceAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Updates the active status of an AI model.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(AIModelResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateModelStatus(
        Guid id,
        [FromBody] UpdateModelStatusRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _modelService.SetModelActiveStatusAsync(id, request.IsActive, cancellationToken);
        return Ok(updated);
    }
}
