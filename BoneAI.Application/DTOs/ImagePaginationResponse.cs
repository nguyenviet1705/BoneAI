namespace BoneAI.Application.DTOs;

public class ImagePaginationResponse
{
    public IReadOnlyList<ImageResponse> Items { get; set; } = Array.Empty<ImageResponse>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}
