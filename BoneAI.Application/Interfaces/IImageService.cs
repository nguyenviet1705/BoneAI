using BoneAI.Application.DTOs;

namespace BoneAI.Application.Interfaces;

public interface IImageService
{
    Task<ImageResponse> UploadAsync(UploadImageRequest request, CancellationToken cancellationToken = default);
    Task<ImagePaginationResponse> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ImageResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ImageFileResult> GetFileAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
