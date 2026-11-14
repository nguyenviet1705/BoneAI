using BoneAI.Application.DTOs;
using BoneAI.Application.Interfaces;
using BoneAI.Domain.Entities;
using BoneAI.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace BoneAI.Application.Services;

public class ImageService : IImageService
{
    private readonly IXRayImageRepository _repository;
    private readonly IImageStorageService _storageService;
    private readonly IImageValidator _validator;
    private readonly ILogger<ImageService> _logger;

    public ImageService(
        IXRayImageRepository repository,
        IImageStorageService storageService,
        IImageValidator validator,
        ILogger<ImageService> logger)
    {
        _repository = repository;
        _storageService = storageService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ImageResponse> UploadAsync(UploadImageRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null || request.FileStream == null)
        {
            throw new ValidationException("Invalid upload request: file stream is null.");
        }

        // 1. Validate image format, size, magic bytes, dimensions, and decoded pixel integrity
        var validationResult = await _validator.ValidateAsync(
            request.FileStream,
            request.FileName,
            request.ContentType,
            request.Length,
            cancellationToken);

        // 2. Write file to physical storage outside wwwroot with unique GUID-based key
        string storageKey;
        try
        {
            storageKey = await _storageService.SaveAsync(
                request.FileStream,
                request.FileName,
                validationResult.DetectedMimeType,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Storage failure: unable to write image file {FileName} to storage.", request.FileName);
            throw;
        }

        // 3. Persist metadata in database with compensation rollback on failure
        var image = new XRayImage
        {
            Id = Guid.NewGuid(),
            FileName = Path.GetFileName(request.FileName),
            FilePath = storageKey,
            ContentType = validationResult.DetectedMimeType,
            FileSize = validationResult.Size > 0 ? validationResult.Size : request.Length,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await _repository.AddAsync(image, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception dbEx)
        {
            _logger.LogError(dbEx, "Database persistence failed for image metadata {FileName}. Initiating storage compensation rollback...", request.FileName);

            try
            {
                var deleted = await _storageService.DeleteAsync(storageKey, CancellationToken.None);
                if (!deleted)
                {
                    _logger.LogCritical("COMPENSATION_FAILURE: Unable to delete orphan file at storage key {StorageKey} after DB failure.", storageKey);
                }
                else
                {
                    _logger.LogInformation("Compensation rollback successfully removed orphan file {StorageKey}.", storageKey);
                }
            }
            catch (Exception cleanupEx)
            {
                _logger.LogCritical(cleanupEx, "COMPENSATION_ERROR: Exception occurred during compensation cleanup of orphan file {StorageKey}.", storageKey);
            }

            throw;
        }

        _logger.LogInformation("Image successfully uploaded and persisted. Id: {ImageId}, FilePath: {FilePath}", image.Id, image.FilePath);
        return MapToResponse(image);
    }

    public async Task<ImagePaginationResponse> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : (pageSize > 100 ? 100 : pageSize);

        var (items, totalCount) = await _repository.GetPagedAsync(page, pageSize, cancellationToken);

        return new ImagePaginationResponse
        {
            Items = items.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ImageResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var image = await _repository.GetByIdAsync(id, cancellationToken);
        if (image == null)
        {
            throw new NotFoundException($"Image with ID '{id}' was not found.");
        }

        return MapToResponse(image);
    }

    public async Task<ImageFileResult> GetFileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var image = await _repository.GetByIdAsync(id, cancellationToken);
        if (image == null)
        {
            throw new NotFoundException($"Image with ID '{id}' was not found.");
        }

        var stream = await _storageService.GetAsync(image.FilePath, cancellationToken);
        if (stream == null)
        {
            _logger.LogError("Physical image file missing from storage for image ID {ImageId} at path {FilePath}", id, image.FilePath);
            throw new NotFoundException($"Physical file for image '{id}' was not found on the server.");
        }

        return new ImageFileResult
        {
            Stream = stream,
            ContentType = image.ContentType,
            FileName = image.FileName
        };
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var image = await _repository.GetByIdAsync(id, cancellationToken);
        if (image == null)
        {
            throw new NotFoundException($"Image with ID '{id}' was not found.");
        }

        // Check if any Analysis records reference this image
        var hasReferences = await _repository.HasAnalysisReferencesAsync(id, cancellationToken);
        if (hasReferences)
        {
            _logger.LogWarning("Deletion rejected: Image {ImageId} has existing analysis references.", id);
            throw new ConflictException($"Image with ID '{id}' cannot be deleted because it is referenced by existing analysis records.");
        }

        // Safe deletion strategy:
        // 1. Delete the physical file first.
        // If physical deletion fails, abort and preserve the database metadata to prevent broken state.
        bool fileDeleted;
        try
        {
            fileDeleted = await _storageService.DeleteAsync(image.FilePath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Physical file deletion failed for storage key {StorageKey}. Aborting database deletion to preserve consistency.", image.FilePath);
            throw new InvalidOperationException("Failed to delete physical image file. Deletion aborted and metadata preserved.", ex);
        }

        if (!fileDeleted)
        {
            _logger.LogWarning("Physical file {StorageKey} could not be confirmed deleted or was already removed.", image.FilePath);
        }

        // 2. Delete database metadata
        try
        {
            await _repository.DeleteAsync(image, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Image {ImageId} successfully deleted from storage and database.", id);
        }
        catch (Exception dbEx)
        {
            _logger.LogCritical(dbEx, "DELETION_INCONSISTENCY: Physical file {StorageKey} was deleted, but database record {ImageId} could not be removed.", image.FilePath, image.Id);
            throw;
        }
    }

    private static ImageResponse MapToResponse(XRayImage image)
    {
        return new ImageResponse
        {
            Id = image.Id,
            FileName = image.FileName,
            ContentType = image.ContentType,
            FileSize = image.FileSize,
            CreatedAt = image.CreatedAt,
            FileUrl = $"/api/images/{image.Id}/file"
        };
    }
}
