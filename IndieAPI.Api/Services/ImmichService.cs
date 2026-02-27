using IndieAPI.Api.Interfaces;
using IndieAPI.Api.Models;

namespace IndieAPI.Api.Services;

public class ImmichService : IImmichService
{
    private readonly HttpClient _httpClient;
    private readonly string[] _albumIds;

    public ImmichService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        // Read the list of Album IDs from configuration
        _albumIds = config.GetSection("Immich:AlbumIds").Get<string[]>() ?? Array.Empty<string>();
    }

    public async Task<IEnumerable<ArtWorkResponse>> GetPagedArtAsync(int page, int pageSize)
    {
        var allAssets = new List<ImmichAsset>();

        foreach (var id in _albumIds)
        {
            var album = await _httpClient.GetFromJsonAsync<ImmichAlbumResponse>($"/api/albums/{id}");
            if (album?.Assets != null)
            {
                allAssets.AddRange(album.Assets);
            }
        }

        // Sort by date descending and paginate
        return allAssets
            .OrderByDescending(a => a.FileCreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ArtWorkResponse(
                a.Id, 
                a.FileCreatedAt, 
                $"/api/art/image/{a.Id}" // This points to our proxy endpoint
            ));
    }

    public async Task<ImageFile> GetImageThumbnailAsync(string assetId)
    {
        // 1. Get asset metadata to check if it's animated
        var metadata = await _httpClient.GetFromJsonAsync<ImmichAsset>($"/api/assets/{assetId}");
        
        bool isAnimated = metadata?.OriginalMimeType == "image/gif" || 
                        metadata?.OriginalMimeType == "image/webp"; // WebP can also be animated

        // 2. Choose the endpoint: 'original' for animations, 'thumbnail' for static art
        string endpoint = isAnimated 
            ? $"/api/assets/{assetId}/original" 
            : $"/api/assets/{assetId}/thumbnail?size=preview"; // 'preview' is slightly larger/better than 'thumbnail'

        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
        var stream = await response.Content.ReadAsStreamAsync();

        return new ImageFile(stream, contentType);
    }
}