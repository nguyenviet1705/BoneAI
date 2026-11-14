using BoneAI.Application.DTOs;
using BoneAI.Application.Interfaces;
using BoneAI.Domain.Entities;
using BoneAI.Domain.Enums;
using BoneAI.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BoneAI.Infrastructure.BackgroundServices;

public class AnalysisBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AIServiceOptions _options;
    private readonly ILogger<AnalysisBackgroundWorker> _logger;

    public AnalysisBackgroundWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<AIServiceOptions> options,
        ILogger<AnalysisBackgroundWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AnalysisBackgroundWorker started. Polling for pending inference jobs.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processedJob = await ProcessNextJobAsync(stoppingToken);
                if (!processedJob)
                {
                    // No job claimed, wait briefly before next poll
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected exception in AnalysisBackgroundWorker polling loop: {Message}", ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }

        _logger.LogInformation("AnalysisBackgroundWorker stopped.");
    }

    public async Task<bool> ProcessNextJobAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var runRepo = scope.ServiceProvider.GetRequiredService<IAnalysisRunRepository>();
        var analysisRepo = scope.ServiceProvider.GetRequiredService<IAnalysisRepository>();
        var storageService = scope.ServiceProvider.GetRequiredService<IImageStorageService>();
        var aiClient = scope.ServiceProvider.GetRequiredService<IAIServiceClient>();

        // Atomically claim next job with a 2-minute processing lease
        var run = await runRepo.ClaimNextPendingRunAsync(TimeSpan.FromMinutes(2), cancellationToken);
        if (run == null)
        {
            return false;
        }

        _logger.LogInformation("Claimed AnalysisRun {RunId} for Analysis {AnalysisId}, Model: {ModelName}",
            run.Id, run.AnalysisId, run.AIModel?.Name);

        var analysis = run.Analysis;
        var xRayImage = analysis?.XRayImage;

        if (xRayImage == null)
        {
            _logger.LogError("AnalysisRun {RunId} failed: associated XRayImage metadata is null.", run.Id);
            await FailRunAsync(run, analysis, "Associated X-ray image metadata was not found.", runRepo, cancellationToken);
            return true;
        }

        // Retrieve physical image stream from storage
        await using var imageStream = await storageService.GetAsync(xRayImage.FilePath, cancellationToken);
        if (imageStream == null)
        {
            _logger.LogError("AnalysisRun {RunId} failed: physical file missing at {FilePath}.", run.Id, xRayImage.FilePath);
            await FailRunAsync(run, analysis, $"Original image file was not found in storage at '{xRayImage.FilePath}'.", runRepo, cancellationToken);
            return true;
        }

        var modelKey = run.AIModel?.Name ?? "mock-bone-fracture-v1";
        var modelVersion = run.AIModel?.Version;

        var request = new InferenceClientRequest
        {
            AnalysisRunId = run.Id,
            ModelKey = modelKey,
            ModelVersion = modelVersion,
            ImageStream = imageStream,
            FileName = xRayImage.FileName,
            ContentType = xRayImage.ContentType
        };

        try
        {
            var response = await aiClient.ExecuteInferenceAsync(request, cancellationToken);

            // Validate and build detection entities
            var detections = new List<Detection>();
            foreach (var d in response.Detections)
            {
                // Ensure bounding box coordinates are clamped and valid
                var x = Math.Max(0m, Math.Min(d.X, (decimal)response.ImageWidth));
                var y = Math.Max(0m, Math.Min(d.Y, (decimal)response.ImageHeight));
                var width = Math.Max(0m, Math.Min(d.Width, (decimal)response.ImageWidth - x));
                var height = Math.Max(0m, Math.Min(d.Height, (decimal)response.ImageHeight - y));
                var confidence = Math.Clamp(d.Confidence, 0m, 1m);

                detections.Add(new Detection
                {
                    Id = Guid.NewGuid(),
                    AnalysisRunId = run.Id,
                    ClassName = d.ClassName,
                    Confidence = confidence,
                    X = x,
                    Y = y,
                    Width = width,
                    Height = height
                });
            }

            // Transactionally persist detections and update statuses
            await runRepo.SaveDetectionsAsync(run.Id, detections, cancellationToken);

            run.Status = AnalysisRunStatus.Completed;
            run.CompletedAt = DateTime.UtcNow;
            run.ProcessingTimeMs = response.ProcessingTimeMs;
            run.ErrorMessage = null;
            run.LeaseUntil = null;

            if (analysis != null)
            {
                analysis.Status = AnalysisStatus.Completed;
            }

            await runRepo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully completed AnalysisRun {RunId} with {Count} detections in {Ms}ms",
                run.Id, detections.Count, response.ProcessingTimeMs);

            return true;
        }
        catch (Exception ex) when (IsTransient(ex))
        {
            if (run.RetryCount < _options.MaxRetryAttempts)
            {
                run.RetryCount++;
                var backoffMs = _options.InitialRetryDelayMs * (int)Math.Pow(2, run.RetryCount - 1);
                run.Status = AnalysisRunStatus.Pending; // Return to pending for retry
                run.LeaseUntil = DateTime.UtcNow.AddMilliseconds(backoffMs);
                run.ErrorMessage = $"Transient error on attempt {run.RetryCount}: {ex.Message}";

                _logger.LogWarning("Transient failure processing Run {RunId}. Scheduling retry {Attempt}/{Max} after {Delay}ms. Error: {Message}",
                    run.Id, run.RetryCount, _options.MaxRetryAttempts, backoffMs, ex.Message);

                await runRepo.SaveChangesAsync(cancellationToken);
            }
            else
            {
                _logger.LogError(ex, "AnalysisRun {RunId} failed permanently: exhausted {Max} retries. Error: {Message}",
                    run.Id, _options.MaxRetryAttempts, ex.Message);
                await FailRunAsync(run, analysis, $"Inference failed after {_options.MaxRetryAttempts} retry attempts: {ex.Message}", runRepo, cancellationToken);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AnalysisRun {RunId} failed with non-transient error: {Message}", run.Id, ex.Message);
            await FailRunAsync(run, analysis, $"Inference failed: {ex.Message}", runRepo, cancellationToken);
            return true;
        }
    }

    private static async Task FailRunAsync(
        AnalysisRun run,
        Analysis? analysis,
        string errorMessage,
        IAnalysisRunRepository runRepo,
        CancellationToken cancellationToken)
    {
        run.Status = AnalysisRunStatus.Failed;
        run.CompletedAt = DateTime.UtcNow;
        run.ErrorMessage = errorMessage.Length > 2000 ? errorMessage[..2000] : errorMessage;
        run.LeaseUntil = null;

        if (analysis != null)
        {
            analysis.Status = AnalysisStatus.Failed;
        }

        await runRepo.SaveChangesAsync(cancellationToken);
    }

    private static bool IsTransient(Exception ex)
    {
        return ex is HttpRequestException ||
               ex is TimeoutException ||
               ex is IOException ||
               ex.InnerException is HttpRequestException ||
               ex.InnerException is TimeoutException;
    }
}
