using BoneAI.Application.DTOs;
using BoneAI.Application.Interfaces;
using BoneAI.Domain.Entities;
using BoneAI.Domain.Enums;
using BoneAI.Domain.Services;
using BoneAI.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;

namespace BoneAI.Infrastructure.BackgroundServices;

public class AnalysisBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AIServiceOptions _options;
    private readonly ILogger<AnalysisBackgroundWorker> _logger;
    private readonly SemaphoreSlim _concurrencySemaphore;

    public AnalysisBackgroundWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<AIServiceOptions> options,
        ILogger<AnalysisBackgroundWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
        _concurrencySemaphore = new SemaphoreSlim(
            _options.MaxDegreeOfParallelism > 0 ? _options.MaxDegreeOfParallelism : 4,
            _options.MaxDegreeOfParallelism > 0 ? _options.MaxDegreeOfParallelism : 4);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AnalysisBackgroundWorker started with max concurrency {Concurrency}. Polling for pending inference jobs.",
            _options.MaxDegreeOfParallelism);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _concurrencySemaphore.WaitAsync(stoppingToken);

                // Attempt to claim and spawn job processing
                var jobClaimed = await TryDispatchJobAsync(stoppingToken);
                if (!jobClaimed)
                {
                    _concurrencySemaphore.Release();
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected exception in AnalysisBackgroundWorker dispatch loop: {Message}", ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }

        _logger.LogInformation("AnalysisBackgroundWorker stopped.");
    }

    private async Task<bool> TryDispatchJobAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var runRepo = scope.ServiceProvider.GetRequiredService<IAnalysisRunRepository>();

        // Atomically claim next job with a 2-minute processing lease
        var run = await runRepo.ClaimNextPendingRunAsync(TimeSpan.FromMinutes(2), cancellationToken);
        if (run == null)
        {
            return false;
        }

        var runId = run.Id;
        _logger.LogInformation("Claimed AnalysisRun {RunId} for Analysis {AnalysisId}, Model: {ModelName}",
            runId, run.AnalysisId, run.AIModel?.Name);

        // Process asynchronously on worker thread pool
        _ = Task.Run(async () =>
        {
            try
            {
                using var workerScope = _scopeFactory.CreateScope();
                var scopedRunRepo = workerScope.ServiceProvider.GetRequiredService<IAnalysisRunRepository>();
                var scopedAnalysisRepo = workerScope.ServiceProvider.GetRequiredService<IAnalysisRepository>();
                var scopedStorageService = workerScope.ServiceProvider.GetRequiredService<IImageStorageService>();
                var scopedAiClient = workerScope.ServiceProvider.GetRequiredService<IAIServiceClient>();
                var scopedRenderingService = workerScope.ServiceProvider.GetService<IImageRenderingService>();

                var scopedRun = await scopedRunRepo.GetByIdAsync(runId, cancellationToken);
                if (scopedRun != null)
                {
                    await ExecuteRunInScopeAsync(scopedRun, scopedRunRepo, scopedAnalysisRepo, scopedStorageService, scopedAiClient, scopedRenderingService, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception processing AnalysisRun {RunId}: {Message}", runId, ex.Message);
            }
            finally
            {
                _concurrencySemaphore.Release();
            }
        }, cancellationToken);

        return true;
    }

    public async Task<bool> ProcessNextJobAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var runRepo = scope.ServiceProvider.GetRequiredService<IAnalysisRunRepository>();
        var analysisRepo = scope.ServiceProvider.GetRequiredService<IAnalysisRepository>();
        var storageService = scope.ServiceProvider.GetRequiredService<IImageStorageService>();
        var aiClient = scope.ServiceProvider.GetRequiredService<IAIServiceClient>();
        var renderingService = scope.ServiceProvider.GetService<IImageRenderingService>();

        var run = await runRepo.ClaimNextPendingRunAsync(TimeSpan.FromMinutes(2), cancellationToken);
        if (run == null)
        {
            return false;
        }

        await ExecuteRunInScopeAsync(run, runRepo, analysisRepo, storageService, aiClient, renderingService, cancellationToken);
        return true;
    }

    private async Task ExecuteRunInScopeAsync(
        AnalysisRun run,
        IAnalysisRunRepository runRepo,
        IAnalysisRepository analysisRepo,
        IImageStorageService storageService,
        IAIServiceClient aiClient,
        IImageRenderingService? renderingService,
        CancellationToken cancellationToken)
    {
        var analysis = run.Analysis;
        var xRayImage = analysis?.XRayImage;

        if (xRayImage == null)
        {
            _logger.LogError("AnalysisRun {RunId} failed: associated XRayImage metadata is null.", run.Id);
            await FailRunAsync(run, "Associated X-ray image metadata was not found.", runRepo, analysisRepo, cancellationToken);
            return;
        }

        // Retrieve physical image stream from storage
        await using var imageStream = await storageService.GetAsync(xRayImage.FilePath, cancellationToken);
        if (imageStream == null)
        {
            _logger.LogError("AnalysisRun {RunId} failed: physical file missing at {FilePath}.", run.Id, xRayImage.FilePath);
            await FailRunAsync(run, $"Original image file was not found in storage at '{xRayImage.FilePath}'.", runRepo, analysisRepo, cancellationToken);
            return;
        }

        var modelKey = !string.IsNullOrEmpty(run.AIModel?.ModelKey)
            ? run.AIModel.ModelKey
            : (run.AIModel?.Name ?? "mock-bone-fracture-v1");
        var modelVersion = run.AIModel?.Version;

        // Detect storage image dimensions to verify against inference response
        int? storageWidth = null;
        int? storageHeight = null;
        try
        {
            if (imageStream.CanSeek)
            {
                imageStream.Position = 0;
                var info = Image.Identify(imageStream);
                if (info != null)
                {
                    storageWidth = info.Width;
                    storageHeight = info.Height;
                }
                imageStream.Position = 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not determine image dimensions from storage stream for Run {RunId}", run.Id);
            if (imageStream.CanSeek)
            {
                imageStream.Position = 0;
            }
        }

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

            // Point 4: Verify image dimensions consistency between Storage and AI Service response
            if (storageWidth.HasValue && storageHeight.HasValue)
            {
                if (response.ImageWidth != storageWidth.Value || response.ImageHeight != storageHeight.Value)
                {
                    var mismatchMsg = $"Image dimension mismatch: storage image is {storageWidth.Value}x{storageHeight.Value}, but AI Service returned {response.ImageWidth}x{response.ImageHeight}.";
                    _logger.LogError("AnalysisRun {RunId} dimension mismatch: {Msg}", run.Id, mismatchMsg);
                    await FailRunAsync(run, mismatchMsg, runRepo, analysisRepo, cancellationToken);
                    return;
                }
            }

            // Validate and build detection entities isolated to this run
            var detections = new List<Detection>();
            foreach (var d in response.Detections)
            {
                // Reject completely invalid or degenerate bounding boxes
                if (d.Width <= 0 || d.Height <= 0 || d.X >= response.ImageWidth || d.Y >= response.ImageHeight)
                {
                    _logger.LogWarning("Skipping invalid detection in Run {RunId}: X={X}, Y={Y}, W={W}, H={H}", run.Id, d.X, d.Y, d.Width, d.Height);
                    continue;
                }

                var x = Math.Max(0m, Math.Min(d.X, (decimal)response.ImageWidth));
                var y = Math.Max(0m, Math.Min(d.Y, (decimal)response.ImageHeight));
                var width = Math.Max(0m, Math.Min(d.Width, (decimal)response.ImageWidth - x));
                var height = Math.Max(0m, Math.Min(d.Height, (decimal)response.ImageHeight - y));
                var confidence = Math.Clamp(d.Confidence, 0m, 1m);

                if (width <= 0m || height <= 0m)
                {
                    _logger.LogWarning("Skipping detection clipped to zero dimensions in Run {RunId}", run.Id);
                    continue;
                }

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

            // Transactionally persist detections isolated to this run
            await runRepo.SaveDetectionsAsync(run.Id, detections, cancellationToken);

            if (renderingService != null && detections.Count > 0)
            {
                try
                {
                    var renderedPath = await renderingService.RenderAndSaveRunImageAsync(run.Id, cancellationToken);
                    if (!string.IsNullOrEmpty(renderedPath))
                    {
                        run.ResultImagePath = renderedPath;
                    }
                }
                catch (Exception renderEx)
                {
                    _logger.LogWarning(renderEx, "Non-fatal: failed to generate rendered image for Run {RunId}: {Message}", run.Id, renderEx.Message);
                }
            }

            run.Status = AnalysisRunStatus.Completed;
            run.CompletedAt = DateTime.UtcNow;
            run.ProcessingTimeMs = response.ProcessingTimeMs;
            run.ErrorMessage = null;
            run.LeaseUntil = null;
            run.IsMockResult = response.IsMockResult;
            run.Warning = response.Warning;

            await runRepo.SaveChangesAsync(cancellationToken);

            // Recalculate aggregate status across all sibling runs
            await UpdateAggregateAnalysisStatusAsync(run, runRepo, analysisRepo, cancellationToken);

            _logger.LogInformation("Successfully completed AnalysisRun {RunId} (IsMock={IsMock}) with {Count} detections in {Ms}ms",
                run.Id, run.IsMockResult, detections.Count, response.ProcessingTimeMs);
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
                await FailRunAsync(run, $"Inference failed after {_options.MaxRetryAttempts} retry attempts: {ex.Message}", runRepo, analysisRepo, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AnalysisRun {RunId} failed with non-transient error: {Message}", run.Id, ex.Message);
            await FailRunAsync(run, $"Inference failed: {ex.Message}", runRepo, analysisRepo, cancellationToken);
        }
    }

    private static async Task FailRunAsync(
        AnalysisRun run,
        string errorMessage,
        IAnalysisRunRepository runRepo,
        IAnalysisRepository analysisRepo,
        CancellationToken cancellationToken)
    {
        run.Status = AnalysisRunStatus.Failed;
        run.CompletedAt = DateTime.UtcNow;
        run.ErrorMessage = errorMessage.Length > 2000 ? errorMessage[..2000] : errorMessage;
        run.LeaseUntil = null;

        await runRepo.SaveChangesAsync(cancellationToken);

        // Recalculate aggregate status across all sibling runs
        await UpdateAggregateAnalysisStatusAsync(run, runRepo, analysisRepo, cancellationToken);
    }

    private static async Task UpdateAggregateAnalysisStatusAsync(
        AnalysisRun run,
        IAnalysisRunRepository runRepo,
        IAnalysisRepository analysisRepo,
        CancellationToken cancellationToken)
    {
        var siblingRuns = await runRepo.GetRunsByAnalysisIdAsync(run.AnalysisId, cancellationToken);
        var runsToEvaluate = (siblingRuns != null && siblingRuns.Count > 0) ? siblingRuns : new[] { run };

        var analysis = run.Analysis ?? await analysisRepo.GetByIdAsync(run.AnalysisId, cancellationToken);
        if (analysis != null)
        {
            var calculatedStatus = AnalysisStatusCalculator.Calculate(runsToEvaluate);
            if (analysis.Status != calculatedStatus)
            {
                analysis.Status = calculatedStatus;
                await runRepo.SaveChangesAsync(cancellationToken);
            }
        }
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
