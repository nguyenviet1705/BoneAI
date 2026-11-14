namespace BoneAI.Application.Interfaces;

public record ImageValidationResult(
    bool IsValid,
    string DetectedFormat,
    string DetectedMimeType,
    int Width,
    int Height,
    long Size);

public interface IImageValidator
{
    Task<ImageValidationResult> ValidateAsync(
        Stream stream,
        string fileName,
        string contentType,
        long? declaredLength = null,
        CancellationToken cancellationToken = default);
}
