using BoneAI.Domain.Entities;

namespace BoneAI.Application.Interfaces;

public interface IXRayImageRepository
{
    Task<XRayImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<XRayImage> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task AddAsync(XRayImage image, CancellationToken cancellationToken = default);
    Task<bool> HasAnalysisReferencesAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteAsync(XRayImage image, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
