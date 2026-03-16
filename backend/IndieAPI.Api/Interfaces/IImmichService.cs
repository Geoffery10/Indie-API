using IndieAPI.Api.Models;

namespace IndieAPI.Api.Interfaces;

public record ImageFile(Stream Stream, string ContentType);

public interface IImmichService
{
    Task<IEnumerable<ArtWorkResponse>> GetPagedArtAsync(int page, int pageSize);
    Task<ImageFile> GetImageThumbnailAsync(string assetId);
}