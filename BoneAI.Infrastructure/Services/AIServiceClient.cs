using System.Net.Http.Headers;
using System.Text.Json;
using BoneAI.Application.DTOs;
using BoneAI.Application.Interfaces;
using BoneAI.Domain.Exceptions;
using BoneAI.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BoneAI.Infrastructure.Services;

public class AIServiceClient : IAIServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly AIServiceOptions _options;
    private readonly ILogger<AIServiceClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AIServiceClient(
        HttpClient httpClient,
        IOptions<AIServiceOptions> options,
        ILogger<AIServiceClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        if (!string.IsNullOrEmpty(_options.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        }
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.HealthCheckTimeoutSeconds));

            var response = await _httpClient.GetAsync("health", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI Service liveness check failed at {BaseAddress}health: {Message}", _httpClient.BaseAddress, ex.Message);
            return false;
        }
    }

    public async Task<bool> CheckReadinessAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.HealthCheckTimeoutSeconds));

            var response = await _httpClient.GetAsync("ready", cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var content = await response.Content.ReadAsStringAsync(cts.Token);
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("isReady", out var isReadyProp))
            {
                return isReadyProp.GetBoolean();
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI Service readiness check failed at {BaseAddress}ready: {Message}", _httpClient.BaseAddress, ex.Message);
            return false;
        }
    }

    public async Task<InferenceClientResponse> ExecuteInferenceAsync(
        InferenceClientRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calling AI Service inference for Run {RunId}, Model {ModelKey}", request.AnalysisRunId, request.ModelKey);

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(request.AnalysisRunId.ToString()), "analysisRunId");
        content.Add(new StringContent(request.ModelKey), "modelKey");

        if (!string.IsNullOrEmpty(request.ModelVersion))
        {
            content.Add(new StringContent(request.ModelVersion), "modelVersion");
        }

        if (request.ImageStream.CanSeek)
        {
            request.ImageStream.Position = 0;
        }

        var streamContent = new StreamContent(request.ImageStream);
        var mime = string.IsNullOrWhiteSpace(request.ContentType) ? "image/jpeg" : request.ContentType;
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(mime);
        content.Add(streamContent, "image", string.IsNullOrWhiteSpace(request.FileName) ? "xray.jpg" : request.FileName);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync("api/v1/inference", content, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP connection error while communicating with AI Service: {Message}", ex.Message);
            throw;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout elapsed while waiting for AI Service inference response ({Timeout}s).", _options.TimeoutSeconds);
            throw new TimeoutException($"AI Service inference request timed out after {_options.TimeoutSeconds} seconds.", ex);
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("AI Service inference failed with status {StatusCode}: {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"AI Service returned error {(int)response.StatusCode}: {responseBody}");
        }

        var result = JsonSerializer.Deserialize<RawInferenceResponse>(responseBody, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize AI Service inference response payload.");

        return new InferenceClientResponse
        {
            AnalysisRunId = Guid.TryParse(result.AnalysisRunId, out var parsedId) ? parsedId : request.AnalysisRunId,
            ModelKey = result.ModelKey,
            ModelVersion = result.ModelVersion,
            ImageWidth = result.ImageWidth,
            ImageHeight = result.ImageHeight,
            ProcessingTimeMs = result.ProcessingTimeMs,
            IsMockResult = result.IsMockResult,
            Warning = result.Warning,
            Detections = result.Detections.Select(d => new InferenceDetectionItem
            {
                ClassName = d.ClassName,
                Confidence = d.Confidence,
                X = d.BoundingBox.X,
                Y = d.BoundingBox.Y,
                Width = d.BoundingBox.Width,
                Height = d.BoundingBox.Height
            }).ToList()
        };
    }

    private class RawBoundingBox
    {
        public decimal X { get; set; }
        public decimal Y { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
    }

    private class RawDetection
    {
        public string ClassName { get; set; } = string.Empty;
        public decimal Confidence { get; set; }
        public RawBoundingBox BoundingBox { get; set; } = new();
    }

    private class RawInferenceResponse
    {
        public string AnalysisRunId { get; set; } = string.Empty;
        public string ModelKey { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        public long ProcessingTimeMs { get; set; }
        public List<RawDetection> Detections { get; set; } = new();
        public bool IsMockResult { get; set; }
        public string? Warning { get; set; }
    }
}
