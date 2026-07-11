using System.Text.Json.Serialization;

namespace IndieAPI.Api.Models;

public class ImmichAsset
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("fileCreatedAt")]
    public DateTime FileCreatedAt { get; set; }

    [JsonPropertyName("exifImageWidth")]
    public int ExifImageWidth { get; set; }

    [JsonPropertyName("exifImageHeight")]
    public int ExifImageHeight { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // "IMAGE" or "VIDEO"

    [JsonPropertyName("originalMimeType")]
    public string OriginalMimeType { get; set; } = string.Empty; // e.g. "image/gif"
}

// Response from Immich v3 POST /api/search/metadata when filtered by albumIds.
// In v3 the per-album assets endpoint was removed; the search endpoint is the
// canonical way to enumerate album contents.
public class ImmichSearchResponse
{
    [JsonPropertyName("assets")]
    public ImmichSearchAssets Assets { get; set; } = new();
}

public class ImmichSearchAssets
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("items")]
    public List<ImmichAsset> Items { get; set; } = new();
}

// Our custom response for your website
public record ArtWorkResponse(string Id, DateTime Date, string ImageUrl);
