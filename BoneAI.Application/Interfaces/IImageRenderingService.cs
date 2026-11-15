namespace BoneAI.Application.Interfaces;

public interface IImageRenderingService
{
    Task<string?> RenderAndSaveRunImageAsync(Guid runId, CancellationToken cancellationToken = default);
}
