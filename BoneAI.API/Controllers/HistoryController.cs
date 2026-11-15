using BoneAI.API.Models;
using BoneAI.Application.DTOs;
using BoneAI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoneAI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HistoryController : ControllerBase
{
    private readonly IHistoryService _historyService;
    private readonly ILogger<HistoryController> _logger;

    public HistoryController(IHistoryService historyService, ILogger<HistoryController> logger)
    {
        _historyService = historyService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paginated list of analysis history items with optional filtering by status, model, and sorting.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(AnalysisHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetList([FromQuery] GetHistoryQuery query, CancellationToken cancellationToken)
    {
        var response = await _historyService.GetHistoryAsync(query, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Retrieves complete historical analysis details and results for a specific analysis session.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AnalysisResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetail(Guid id, CancellationToken cancellationToken)
    {
        var response = await _historyService.GetHistoryDetailAsync(id, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Soft-deletes an analysis from history, hides it from list views, and safely cleans up rendered images in storage.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _historyService.DeleteHistoryAsync(id, cancellationToken);
        return NoContent();
    }
}
