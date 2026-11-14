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

    public async Task<IReadOnlyList<AIServiceModelDto>> GetModelCatalogAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("api/v1/models", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("models", out var modelsElem))
            {
                var models = JsonSerializer.Deserialize<List<AIServiceModelDto>>(modelsElem.GetRawText(), JsonOptions);
                return models ?? new List<AIServiceModelDto>();
            }

            return Array.Empty<AIServiceModelDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve model catalog from AI Service: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<AIServiceModelDto?> GetModelDetailsAsync(string modelKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/models/{Uri.EscapeDataString(modelKey)}", cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<AIServiceModelDto>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve details for model '{ModelKey}' from AI Service: {Message}", modelKey, ex.Message);
            throw;
        }
    }

    public async Task<AIServiceModelSyncResultDto> TriggerModelDiscoveryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync("api/v1/models/discover", null, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<AIServiceModelSyncResultDto>(json, JsonOptions) ?? new AIServiceModelSyncResultDto();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to trigger model discovery on AI Service: {Message}", ex.Message);
            throw;
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
        var mediaType = string.IsNullOrWhiteSpace(request.ContentType) ? "image/jpeg" : request.ContentType;
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        content.Add(streamContent, "image", request.FileName);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync("api/v1/inference", content, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP network failure connecting to AI Service: {Message}", ex.Message);
            throw;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "AI Service inference timed out after {Timeout} seconds", _options.TimeoutSeconds);
            throw new TimeoutException($"Inference service timed out after {_options.TimeoutSeconds} seconds.", ex);
        }

        var responseString = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("AI Service responded with status {StatusCode}: {Response}", (int)response.StatusCode, responseString);

            string? detailMessage = null;
            try
            {
                using var errDoc = JsonDocument.Parse(responseString);
                if (errDoc.RootElement.TryGetProperty("message", out var msgProp))
                {
                    detailMessage = msgProp.GetString();
                }
                else if (errDoc.RootElement.TryGetProperty("detail", out var dtProp))
                {
                    detailMessage = dtProp.GetString();
                }
            }
            catch
            {
                detailMessage = responseString;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
                response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
            {
                throw new ValidationException(detailMessage ?? "AI Service rejected image payload.");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new NotFoundException(detailMessage ?? $"Requested model '{request.ModelKey}' was not found in AI Service registry.");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                throw new InvalidOperationException(detailMessage ?? $"Model '{request.ModelKey}' is registered but not loaded/ready.");
            }

            throw new HttpRequestException($"AI Service error [{(int)response.StatusCode}]: {detailMessage ?? response.ReasonPhrase}");
        }

        using var doc = JsonDocument.Parse(responseString);
        var root = doc.RootElement;

        var clientResponse = new InferenceClientResponse
        {
            AnalysisRunId = root.GetProperty("analysisRunId").GetGuid(),
            ModelKey = root.GetProperty("modelKey").GetString() ?? request.ModelKey,
            ModelVersion = root.GetProperty("modelVersion").GetString() ?? string.Empty,
            ImageWidth = root.GetProperty("imageWidth").GetInt32(),
            ImageHeight = root.GetProperty("imageHeight").GetInt32(),
            ProcessingTimeMs = root.GetProperty("processingTimeMs").GetInt64()
        };

        if (root.TryGetProperty("detections", out var detectionsArray) && detectionsArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var elem in detectionsArray.EnumerateArray())
            {
                var className = elem.GetProperty("className").GetString() ?? "fracture";
                var confidence = elem.GetProperty("confidence").GetDecimal();
                var box = elem.GetProperty("boundingBox");

                clientResponse.Detections.Add(new InferenceDetectionItem
                {
                    ClassName = className,
                    Confidence = confidence,
                    X = box.GetProperty("x").GetDecimal(),
                    Y = box.GetProperty("y").GetDecimal(),
                    Width = box.GetProperty("width").GetDecimal(),
                    Height = box.GetProperty("height").GetDecimal()
                });
            }
        }

        return clientResponse;
    }
}
