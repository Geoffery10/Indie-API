using System.Net.Http.Json;
using IndieAPI.Api.Interfaces;
using IndieAPI.Api.Models;

namespace IndieAPI.Api.Services;

public class ImmichService : IImmichService
{
    // Immich v3 caps /api/search/metadata results at 1000 per call.
    private const int ImmichV3SearchPageSize = 1000;

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
        // v3 dropped GET /api/albums/{id} returning an embedded `assets[]` array.
        // The supported way to enumerate an album's contents in v3 is
        // POST /api/search/metadata with `albumIds` as a filter.
        var allAssets = await FetchAllAssetsAsync();

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

    private async Task<List<ImmichAsset>> FetchAllAssetsAsync()
    {
        var allAssets = new List<ImmichAsset>();

        // Skip the call entirely if no albums are configured. This matches the
        // v2 behavior (empty album list -> empty result) and avoids a noisy
        // Immich API error in dev environments with no Immich:AlbumIds.
        if (_albumIds.Length == 0)
        {
            return allAssets;
        }

        var page = 1;
        while (true)
        {
            var body = new
            {
                albumIds = _albumIds,
                type = "IMAGE",
                size = ImmichV3SearchPageSize,
                page = page,
                order = "desc"
            };

            var response = await _httpClient.PostAsJsonAsync("/api/search/metadata", body);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<ImmichSearchResponse>();
            var items = payload?.Assets?.Items;
            if (items == null || items.Count == 0)
            {
                break;
            }

            allAssets.AddRange(items);

            // v3 returns `assets.total` as the count matching the filter, so we
            // can stop as soon as we've collected everything without an extra
            // round trip.
            var total = payload?.Assets?.Total ?? 0;
            if (allAssets.Count >= total)
            {
                break;
            }

            page++;
        }

        return allAssets;
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
