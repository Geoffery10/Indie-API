using System.Text.Json.Serialization;

namespace IndieAPI.Api.Models;

public class ImmichAsset
{
    public string Id { get; set; } = string.Empty;
    public DateTime FileCreatedAt { get; set; }
    public int ExifImageWidth { get; set; }
    public int ExifImageHeight { get; set; }
    public string Type { get; set; } = string.Empty; // "IMAGE" or "VIDEO"
    public string OriginalMimeType { get; set; } = string.Empty; // e.g. "image/gif"
}

public class ImmichAlbumResponse
{
    public string Id { get; set; } = string.Empty;
    public string AlbumName { get; set; } = string.Empty;
    public List<ImmichAsset> Assets { get; set; } = new();
}

// Our custom response for your website
public record ArtWorkResponse(string Id, DateTime Date, string ImageUrl);