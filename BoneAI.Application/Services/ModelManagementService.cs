using BoneAI.Application.DTOs;
using BoneAI.Application.Interfaces;
using BoneAI.Domain.Entities;
using BoneAI.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace BoneAI.Application.Services;

public class ModelManagementService : IModelManagementService
{
    private readonly IAIModelRepository _modelRepo;
    private readonly IAIServiceClient _aiClient;
    private readonly ILogger<ModelManagementService> _logger;

    public ModelManagementService(
        IAIModelRepository modelRepo,
        IAIServiceClient aiClient,
        ILogger<ModelManagementService> logger)
    {
        _modelRepo = modelRepo;
        _aiClient = aiClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AIModelResponse>> GetModelsAsync(
        bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        var models = activeOnly == true
            ? await _modelRepo.GetActiveModelsAsync(cancellationToken)
            : await _modelRepo.GetAllAsync(cancellationToken);

        return models.Select(MapToResponse).ToList();
    }

    public async Task<AIModelResponse?> GetModelByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var model = await _modelRepo.GetByIdAsync(id, cancellationToken);
        return model == null ? null : MapToResponse(model);
    }

    public async Task<AIModelStatusResponse?> GetModelStatusAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var model = await _modelRepo.GetByIdAsync(id, cancellationToken);
        if (model == null)
        {
            return null;
        }

        string? aiServiceStatus = null;
        string? aiServiceErrorMessage = null;

        try
        {
            var details = await _aiClient.GetModelDetailsAsync(model.ModelKey, cancellationToken);
            if (details != null)
            {
                aiServiceStatus = details.Status;
                aiServiceErrorMessage = details.ErrorMessage;
            }
            else
            {
                aiServiceStatus = "NotFoundInAIService";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch live status from AI service for model {ModelKey}", model.ModelKey);
            aiServiceStatus = "AIServiceUnreachable";
            aiServiceErrorMessage = ex.Message;
        }

        return new AIModelStatusResponse
        {
            Id = model.Id,
            ModelKey = model.ModelKey,
            Version = model.Version,
            IsActive = model.IsActive,
            IsAvailable = model.IsAvailable,
            AiServiceStatus = aiServiceStatus,
            AiServiceErrorMessage = aiServiceErrorMessage,
            CheckedAt = DateTime.UtcNow
        };
    }

    public async Task<SyncModelsResponse> SyncModelsWithAIServiceAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Beginning model catalog synchronization with Python AI Service.");

        IReadOnlyList<AIServiceModelDto> catalog;
        try
        {
            // Optionally trigger manifest rescan first
            await _aiClient.TriggerModelDiscoveryAsync(cancellationToken);
            catalog = await _aiClient.GetModelCatalogAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve model catalog from AI Service: {Message}", ex.Message);
            throw new ValidationException($"Cannot synchronize models: AI Service communication failed ({ex.Message})");
        }

        int created = 0;
        int updated = 0;
        int unchanged = 0;

        foreach (var remote in catalog)
        {
            var isAvailable = string.Equals(remote.Status, "Loaded", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(remote.Status, "Available", StringComparison.OrdinalIgnoreCase);

            var existing = await _modelRepo.GetByModelKeyAndVersionAsync(remote.ModelKey, remote.Version, cancellationToken);
            if (existing == null)
            {
                // Create new historical AIModel record
                var newModel = new AIModel
                {
                    Id = Guid.NewGuid(),
                    ModelKey = remote.ModelKey,
                    Name = remote.ModelName,
                    Version = remote.Version,
                    Framework = remote.Framework,
                    Description = remote.Description,
                    ArtifactPath = remote.ArtifactPath,
                    IsActive = remote.IsActive,
                    IsAvailable = isAvailable,
                    CreatedAt = DateTime.UtcNow
                };

                await _modelRepo.AddAsync(newModel, cancellationToken);
                created++;
                _logger.LogInformation("Synchronized new model '{ModelKey}' v{Version} (ID: {ModelId})",
                    newModel.ModelKey, newModel.Version, newModel.Id);
            }
            else
            {
                // Update availability or description while preserving existing ID
                var changed = false;
                if (existing.IsAvailable != isAvailable)
                {
                    existing.IsAvailable = isAvailable;
                    changed = true;
                }
                if (existing.Name != remote.ModelName)
                {
                    existing.Name = remote.ModelName;
                    changed = true;
                }
                if (existing.Description != remote.Description && remote.Description != null)
                {
                    existing.Description = remote.Description;
                    changed = true;
                }

                if (changed)
                {
                    updated++;
                }
                else
                {
                    unchanged++;
                }
            }
        }

        await _modelRepo.SaveChangesAsync(cancellationToken);

        var allModels = await _modelRepo.GetAllAsync(cancellationToken);

        _logger.LogInformation("Model catalog synchronization complete. Discovered: {Discovered}, Created: {Created}, Updated: {Updated}, Unchanged: {Unchanged}",
            catalog.Count, created, updated, unchanged);

        return new SyncModelsResponse
        {
            DiscoveredInAiService = catalog.Count,
            CreatedInDatabase = created,
            UpdatedInDatabase = updated,
            UnchangedInDatabase = unchanged,
            Models = allModels.Select(MapToResponse).ToList()
        };
    }

    public async Task<AIModelResponse> SetModelActiveStatusAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var model = await _modelRepo.GetByIdAsync(id, cancellationToken);
        if (model == null)
        {
            throw new NotFoundException($"AI Model with ID '{id}' was not found.");
        }

        model.IsActive = isActive;
        await _modelRepo.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Model {ModelKey} v{Version} active status set to {IsActive}",
            model.ModelKey, model.Version, isActive);

        return MapToResponse(model);
    }

    private static AIModelResponse MapToResponse(AIModel m) => new()
    {
        Id = m.Id,
        ModelKey = m.ModelKey,
        Name = m.Name,
        Version = m.Version,
        Framework = m.Framework,
        Description = m.Description,
        IsActive = m.IsActive,
        IsAvailable = m.IsAvailable,
        CreatedAt = m.CreatedAt
    };
}
