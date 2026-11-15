using BoneAI.Application.DTOs;
using BoneAI.Application.Interfaces;
using BoneAI.Domain.Entities;
using BoneAI.Domain.Enums;
using BoneAI.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace BoneAI.Application.Services;

public class AnalysisService : IAnalysisService
{
    private readonly IAnalysisRepository _analysisRepo;
    private readonly IXRayImageRepository _imageRepo;
    private readonly IAIModelRepository _modelRepo;
    private readonly IImageStorageService _storageService;
    private readonly ILogger<AnalysisService> _logger;

    public AnalysisService(
        IAnalysisRepository analysisRepo,
        IXRayImageRepository imageRepo,
        IAIModelRepository modelRepo,
        IImageStorageService storageService,
        ILogger<AnalysisService> logger)
    {
        _analysisRepo = analysisRepo;
        _imageRepo = imageRepo;
        _modelRepo = modelRepo;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<AnalysisSubmissionResponse> SubmitAsync(
        SubmitAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ValidationException("Analysis request payload is missing.");
        }

        // 1. Validate XRayImage metadata exists
        var image = await _imageRepo.GetByIdAsync(request.XRayImageId, cancellationToken);
        if (image == null)
        {
            throw new NotFoundException($"X-ray image with ID '{request.XRayImageId}' was not found.");
        }

        // 2. Validate that physical file exists in storage
        var fileExists = await _storageService.ExistsAsync(image.FilePath, cancellationToken);
        if (!fileExists)
        {
            _logger.LogError("Physical image file is missing at {FilePath} for image ID {ImageId}", image.FilePath, image.Id);
            throw new NotFoundException($"Physical image file for '{request.XRayImageId}' was not found in server storage.");
        }

        // 3. Resolve Analysis Mode
        var mode = ResolveAnalysisMode(request);

        // 4. Resolve Eligible Models according to Mode
        var modelsToRun = await ResolveModelsForExecutionAsync(mode, request, cancellationToken);

        // 5. Idempotency Check
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await _analysisRepo.GetByIdempotencyKeyAsync(request.IdempotencyKey.Trim(), cancellationToken);
            if (existing != null)
            {
                _logger.LogInformation("Idempotent request matched existing Analysis {AnalysisId} for key '{Key}'. Returning existing resource.",
                    existing.Id, request.IdempotencyKey);

                var firstRun = existing.AnalysisRuns.FirstOrDefault();
                return new AnalysisSubmissionResponse
                {
                    AnalysisId = existing.Id,
                    AnalysisRunId = firstRun?.Id ?? Guid.Empty,
                    AnalysisRunIds = existing.AnalysisRuns.Select(r => r.Id).ToList(),
                    Mode = existing.Mode.ToString(),
                    Status = existing.Status.ToString(),
                    StatusUrl = $"/api/analysis/{existing.Id}",
                    CreatedAt = existing.CreatedAt
                };
            }
        }

        // 6. Create Analysis and AnalysisRuns
        var analysisId = Guid.NewGuid();
        var analysis = new Analysis
        {
            Id = analysisId,
            XRayImageId = image.Id,
            Mode = mode,
            Status = AnalysisStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            IdempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey.Trim()
        };

        var runIds = new List<Guid>();
        foreach (var model in modelsToRun)
        {
            var runId = Guid.NewGuid();
            runIds.Add(runId);

            var run = new AnalysisRun
            {
                Id = runId,
                AnalysisId = analysisId,
                AIModelId = model.Id,
                Status = AnalysisRunStatus.Pending,
                RetryCount = 0
            };
            analysis.AnalysisRuns.Add(run);
        }

        await _analysisRepo.AddAsync(analysis, cancellationToken);
        await _analysisRepo.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Analysis {AnalysisId} queued in mode '{Mode}' with {Count} model runs for image {ImageId}",
            analysisId, mode, modelsToRun.Count, image.Id);

        return new AnalysisSubmissionResponse
        {
            AnalysisId = analysisId,
            AnalysisRunId = runIds.FirstOrDefault(),
            AnalysisRunIds = runIds,
            Mode = analysis.Mode.ToString(),
            Status = analysis.Status.ToString(),
            StatusUrl = $"/api/analysis/{analysisId}",
            CreatedAt = analysis.CreatedAt
        };
    }

    public async Task<AnalysisDetailResponse> GetStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var analysis = await _analysisRepo.GetByIdAsync(id, cancellationToken);
        if (analysis == null)
        {
            throw new NotFoundException($"Analysis with ID '{id}' was not found.");
        }

        var runDtos = analysis.AnalysisRuns.Select(r => new AnalysisRunDetailDto
        {
            AnalysisRunId = r.Id,
            AIModelId = r.AIModelId,
            ModelKey = r.AIModel?.ModelKey ?? string.Empty,
            ModelName = r.AIModel?.Name ?? string.Empty,
            ModelVersion = r.AIModel?.Version ?? string.Empty,
            Framework = r.AIModel?.Framework ?? string.Empty,
            Status = r.Status.ToString(),
            StartedAt = r.StartedAt,
            CompletedAt = r.CompletedAt,
            ProcessingTimeMs = r.ProcessingTimeMs,
            RetryCount = r.RetryCount,
            ErrorMessage = r.ErrorMessage,
            IsMockResult = r.IsMockResult,
            Warning = r.Warning,
            Detections = r.Detections.Select(d => new DetectionDto
            {
                Id = d.Id,
                ClassName = d.ClassName,
                Confidence = d.Confidence,
                X = d.X,
                Y = d.Y,
                Width = d.Width,
                Height = d.Height
            }).ToList()
        }).ToList();

        var completedRuns = runDtos.Count(r => r.Status == AnalysisRunStatus.Completed.ToString());
        var failedRuns = runDtos.Count(r => r.Status == AnalysisRunStatus.Failed.ToString());

        return new AnalysisDetailResponse
        {
            AnalysisId = analysis.Id,
            XRayImageId = analysis.XRayImageId,
            Mode = analysis.Mode.ToString(),
            Status = analysis.Status.ToString(),
            CreatedAt = analysis.CreatedAt,
            TotalRunsCount = runDtos.Count,
            CompletedRunsCount = completedRuns,
            FailedRunsCount = failedRuns,
            AnalysisRuns = runDtos
        };
    }

    private static AnalysisMode ResolveAnalysisMode(SubmitAnalysisRequest request)
    {
        if (request.Mode.HasValue)
        {
            return request.Mode.Value;
        }

        // Automatic inference for backward compatibility
        if (request.SelectedModelIds != null && request.SelectedModelIds.Count > 1)
        {
            return AnalysisMode.MultiModel;
        }

        return AnalysisMode.SingleModel;
    }

    private async Task<List<AIModel>> ResolveModelsForExecutionAsync(
        AnalysisMode mode,
        SubmitAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        switch (mode)
        {
            case AnalysisMode.SingleModel:
            {
                var targetId = request.SelectedModelIds?.FirstOrDefault() ?? request.AIModelId;
                if (!targetId.HasValue || targetId.Value == Guid.Empty)
                {
                    throw new ValidationException("Single Model mode requires exactly one selected model ID.");
                }

                if (request.SelectedModelIds != null && request.SelectedModelIds.Count > 1)
                {
                    throw new ValidationException("Single Model mode cannot accept more than one selected model ID.");
                }

                var model = await _modelRepo.GetByIdAsync(targetId.Value, cancellationToken);
                if (model == null)
                {
                    throw new NotFoundException($"AI Model with ID '{targetId.Value}' was not found.");
                }

                if (!model.IsActive)
                {
                    throw new ValidationException($"AI Model '{model.Name}' (v{model.Version}) is currently inactive.");
                }

                if (!model.IsAvailable)
                {
                    throw new ValidationException($"AI Model '{model.Name}' (v{model.Version}) is currently unavailable in the inference service.");
                }

                return new List<AIModel> { model };
            }

            case AnalysisMode.MultiModel:
            {
                var selectedIds = request.SelectedModelIds;
                if (selectedIds == null || selectedIds.Count < 2)
                {
                    throw new ValidationException("Multi-Model mode requires at least two selected model IDs.");
                }

                // Check for duplicate model IDs
                var distinctCount = selectedIds.Distinct().Count();
                if (distinctCount != selectedIds.Count)
                {
                    throw new ValidationException("Duplicate model IDs are not allowed in Multi-Model mode.");
                }

                var models = new List<AIModel>();
                foreach (var id in selectedIds)
                {
                    var model = await _modelRepo.GetByIdAsync(id, cancellationToken);
                    if (model == null)
                    {
                        throw new NotFoundException($"AI Model with ID '{id}' was not found.");
                    }

                    if (!model.IsActive)
                    {
                        throw new ValidationException($"Selected AI Model '{model.Name}' (v{model.Version}) is currently inactive.");
                    }

                    if (!model.IsAvailable)
                    {
                        throw new ValidationException($"Selected AI Model '{model.Name}' (v{model.Version}) is currently unavailable.");
                    }

                    models.Add(model);
                }

                return models;
            }

            case AnalysisMode.CompareAll:
            {
                var activeAvailableModels = await _modelRepo.GetActiveAndAvailableModelsAsync(cancellationToken);
                if (activeAvailableModels.Count == 0)
                {
                    throw new ValidationException("Compare All mode requires at least one active and available AI model in the catalog.");
                }

                return activeAvailableModels.ToList();
            }

            default:
                throw new ValidationException($"Unsupported analysis mode: {mode}");
        }
    }
}
