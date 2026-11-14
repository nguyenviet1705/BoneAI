namespace BoneAI.Application.Interfaces;

public interface IImageStorageService
{
    Task<string> SaveAsync(Stream stream, string originalFileName, string contentType, CancellationToken cancellationToken = default);
    Task<Stream?> GetAsync(string storageKey, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default);
}
