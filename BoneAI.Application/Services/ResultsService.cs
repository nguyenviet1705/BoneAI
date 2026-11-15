using BoneAI.Application.DTOs;
using BoneAI.Application.Interfaces;
using BoneAI.Domain.Entities;
using BoneAI.Domain.Enums;
using BoneAI.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace BoneAI.Application.Services;

public class ResultsService : IResultsService
{
    private readonly IAnalysisRepository _analysisRepo;
    private readonly IAnalysisRunRepository _runRepo;
    private readonly IXRayImageRepository _imageRepo;
    private readonly IImageStorageService _storageService;
    private readonly ILogger<ResultsService> _logger;

    public ResultsService(
        IAnalysisRepository analysisRepo,
        IAnalysisRunRepository runRepo,
        IXRayImageRepository imageRepo,
        IImageStorageService storageService,
        ILogger<ResultsService> logger)
    {
        _analysisRepo = analysisRepo;
        _runRepo = runRepo;
        _imageRepo = imageRepo;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<AnalysisResultDto> GetAnalysisResultAsync(Guid analysisId, CancellationToken cancellationToken = default)
    {
        var analysis = await _analysisRepo.GetByIdAsync(analysisId, cancellationToken);
        if (analysis == null)
        {
            throw new NotFoundException($"Analysis with ID '{analysisId}' was not found.");
        }

        var image = analysis.XRayImage ?? await _imageRepo.GetByIdAsync(analysis.XRayImageId, cancellationToken);

        var originalImageDto = new OriginalImageInfoDto
        {
            ImageId = analysis.XRayImageId,
            FileName = image?.FileName ?? string.Empty,
            ContentType = image?.ContentType ?? "image/jpeg",
            FileSize = image?.FileSize ?? 0,
            OriginalImageUrl = $"/api/images/{analysis.XRayImageId}/file"
        };

        var runDtos = analysis.AnalysisRuns.Select(MapToRunResultDto).ToList();

        var completedRuns = runDtos.Count(r => r.Status == AnalysisRunStatus.Completed.ToString());
        var failedRuns = runDtos.Count(r => r.Status == AnalysisRunStatus.Failed.ToString());

        return new AnalysisResultDto
        {
            AnalysisId = analysis.Id,
            ImageId = analysis.XRayImageId,
            OriginalImage = originalImageDto,
            OverallStatus = analysis.Status.ToString(),
            Mode = analysis.Mode.ToString(),
            CreatedAt = analysis.CreatedAt,
            TotalRunsCount = runDtos.Count,
            CompletedRunsCount = completedRuns,
            FailedRunsCount = failedRuns,
            Runs = runDtos
        };
    }

    public async Task<AnalysisRunResultDto> GetAnalysisRunResultAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await _runRepo.GetByIdAsync(runId, cancellationToken);
        if (run == null)
        {
            throw new NotFoundException($"Analysis run with ID '{runId}' was not found.");
        }

        return MapToRunResultDto(run);
    }

    public async Task<AnalysisComparisonDto> GetAnalysisComparisonAsync(Guid analysisId, CancellationToken cancellationToken = default)
    {
        var analysis = await _analysisRepo.GetByIdAsync(analysisId, cancellationToken);
        if (analysis == null)
        {
            throw new NotFoundException($"Analysis with ID '{analysisId}' was not found.");
        }

        var modelComparisons = analysis.AnalysisRuns.Select(run =>
        {
            var isFailed = run.Status == AnalysisRunStatus.Failed;
            var detections = isFailed
                ? new List<DetectionItemDto>()
                : run.Detections.Select(MapToDetectionDto).ToList();

            double? processingTimeSec = null;
            if (run.Status == AnalysisRunStatus.Completed && run.ProcessingTimeMs.HasValue)
            {
                processingTimeSec = Math.Round((double)run.ProcessingTimeMs.Value / 1000.0, 3);
            }

            decimal? averageConfidence = null;
            if (detections.Count > 0)
            {
                averageConfidence = Math.Round(detections.Average(d => d.ConfidenceScore), 4);
            }

            return new ModelComparisonItemDto
            {
                RunId = run.Id,
                ModelId = run.AIModelId,
                ModelName = run.AIModel?.Name ?? string.Empty,
                ModelKey = run.AIModel?.ModelKey ?? string.Empty,
                Version = run.AIModel?.Version ?? string.Empty,
                Status = run.Status.ToString(),
                ProcessingTime = processingTimeSec,
                DetectionCount = detections.Count,
                AverageConfidence = averageConfidence,
                Detections = detections,
                ResultImageUrl = !string.IsNullOrEmpty(run.ResultImagePath) ? $"/api/results/runs/{run.Id}/image" : null,
                ErrorMessage = run.ErrorMessage,
                IsMockResult = run.IsMockResult,
                Warning = run.Warning
            };
        }).ToList();

        return new AnalysisComparisonDto
        {
            AnalysisId = analysis.Id,
            ImageId = analysis.XRayImageId,
            OverallStatus = analysis.Status.ToString(),
            OriginalImageUrl = $"/api/images/{analysis.XRayImageId}/file",
            Models = modelComparisons
        };
    }

    public async Task<ImageFileResult> GetRunResultImageAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await _runRepo.GetByIdAsync(runId, cancellationToken);
        if (run == null)
        {
            throw new NotFoundException($"Analysis run with ID '{runId}' was not found.");
        }

        if (string.IsNullOrWhiteSpace(run.ResultImagePath))
        {
            throw new NotFoundException($"Result image for analysis run '{runId}' is not available.");
        }

        var stream = await _storageService.GetAsync(run.ResultImagePath, cancellationToken);
        if (stream == null)
        {
            _logger.LogWarning("Result image file missing from storage for run {RunId} at {Path}", runId, run.ResultImagePath);
            throw new NotFoundException($"Result image file for analysis run '{runId}' was not found in storage.");
        }

        var ext = Path.GetExtension(run.ResultImagePath).ToLowerInvariant();
        var contentType = ext == ".png" ? "image/png" : "image/jpeg";

        return new ImageFileResult
        {
            Stream = stream,
            ContentType = contentType,
            FileName = Path.GetFileName(run.ResultImagePath)
        };
    }

    private static AnalysisRunResultDto MapToRunResultDto(AnalysisRun run)
    {
        var isFailed = run.Status == AnalysisRunStatus.Failed;
        var detections = isFailed
            ? new List<DetectionItemDto>()
            : run.Detections.Select(MapToDetectionDto).ToList();

        double? processingTimeSec = null;
        if (run.Status == AnalysisRunStatus.Completed && run.ProcessingTimeMs.HasValue)
        {
            processingTimeSec = Math.Round((double)run.ProcessingTimeMs.Value / 1000.0, 3);
        }

        return new AnalysisRunResultDto
        {
            RunId = run.Id,
            AnalysisId = run.AnalysisId,
            Model = new ModelInfoDto
            {
                ModelId = run.AIModelId,
                ModelName = run.AIModel?.Name ?? string.Empty,
                ModelKey = run.AIModel?.ModelKey ?? string.Empty,
                Version = run.AIModel?.Version ?? string.Empty,
                Framework = run.AIModel?.Framework ?? string.Empty
            },
            Status = run.Status.ToString(),
            ProcessingTime = processingTimeSec,
            ProcessingTimeMs = run.Status == AnalysisRunStatus.Completed ? run.ProcessingTimeMs : null,
            StartedAt = run.StartedAt,
            CompletedAt = run.CompletedAt,
            ErrorMessage = run.ErrorMessage,
            ResultImageUrl = !string.IsNullOrEmpty(run.ResultImagePath) ? $"/api/results/runs/{run.Id}/image" : null,
            DetectionCount = detections.Count,
            IsMockResult = run.IsMockResult,
            Warning = run.Warning,
            Detections = detections
        };
    }

    private static DetectionItemDto MapToDetectionDto(Detection d)
    {
        return new DetectionItemDto
        {
            DetectionId = d.Id,
            ClassLabel = d.ClassName,
            ConfidenceScore = d.Confidence,
            BoundingBox = new BoundingBoxDto
            {
                X = d.X,
                Y = d.Y,
                Width = d.Width,
                Height = d.Height
            }
        };
    }
}
