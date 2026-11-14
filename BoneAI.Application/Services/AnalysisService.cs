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

        // 3. Validate AIModel exists and is active
        var model = await _modelRepo.GetByIdAsync(request.AIModelId, cancellationToken);
        if (model == null)
        {
            throw new NotFoundException($"AI Model with ID '{request.AIModelId}' was not found.");
        }

        if (!model.IsActive)
        {
            throw new ValidationException($"AI Model '{model.Name}' (v{model.Version}) is currently inactive.");
        }

        // 4. Idempotency Check
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await _analysisRepo.GetByIdempotencyKeyAsync(request.IdempotencyKey.Trim(), cancellationToken);
            if (existing != null)
            {
                _logger.LogInformation("Idempotent request matched existing Analysis {AnalysisId} for key '{Key}'. Returning existing resource.",
                    existing.Id, request.IdempotencyKey);

                var primaryRun = existing.AnalysisRuns.FirstOrDefault();
                return new AnalysisSubmissionResponse
                {
                    AnalysisId = existing.Id,
                    AnalysisRunId = primaryRun?.Id ?? Guid.Empty,
                    Status = existing.Status.ToString(),
                    StatusUrl = $"/api/analysis/{existing.Id}",
                    CreatedAt = existing.CreatedAt
                };
            }
        }

        // 5. Create Analysis and AnalysisRun
        var analysisId = Guid.NewGuid();
        var runId = Guid.NewGuid();

        var run = new AnalysisRun
        {
            Id = runId,
            AnalysisId = analysisId,
            AIModelId = model.Id,
            Status = AnalysisRunStatus.Pending,
            RetryCount = 0
        };

        var analysis = new Analysis
        {
            Id = analysisId,
            XRayImageId = image.Id,
            Status = AnalysisStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            IdempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey.Trim()
        };

        analysis.AnalysisRuns.Add(run);

        await _analysisRepo.AddAsync(analysis, cancellationToken);
        await _analysisRepo.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Analysis {AnalysisId} and Run {RunId} successfully queued for image {ImageId} with model {ModelName}",
            analysisId, runId, image.Id, model.Name);

        return new AnalysisSubmissionResponse
        {
            AnalysisId = analysisId,
            AnalysisRunId = runId,
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
            ModelName = r.AIModel?.Name ?? string.Empty,
            ModelVersion = r.AIModel?.Version ?? string.Empty,
            Status = r.Status.ToString(),
            StartedAt = r.StartedAt,
            CompletedAt = r.CompletedAt,
            ProcessingTimeMs = r.ProcessingTimeMs,
            ErrorMessage = r.ErrorMessage,
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

        return new AnalysisDetailResponse
        {
            AnalysisId = analysis.Id,
            XRayImageId = analysis.XRayImageId,
            Status = analysis.Status.ToString(),
            CreatedAt = analysis.CreatedAt,
            AnalysisRuns = runDtos
        };
    }
}
