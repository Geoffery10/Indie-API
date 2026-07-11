using System;
using System.Collections.Generic;
using System.Text.Json;
using IndieAPI.Api.Models;
using Xunit;

namespace IndieAPI.Tests;

public class ImmichModelsTests
{
    [Fact]
    public void ImmichAsset_Deserialize_ShouldPopulateAllFields()
    {
        // Immich v3 returns camelCase JSON. The model uses [JsonPropertyName]
        // attributes that map camelCase fields to PascalCase properties.
        var json = "{\"id\":\"asset123\",\"fileCreatedAt\":\"2023-01-02T03:04:05Z\",\"exifImageWidth\":800,\"exifImageHeight\":600,\"type\":\"IMAGE\",\"originalMimeType\":\"image/jpeg\"}";
        var asset = JsonSerializer.Deserialize<ImmichAsset>(json);
        Assert.NotNull(asset);
        Assert.Equal("asset123", asset!.Id);
        Assert.Equal(DateTime.Parse("2023-01-02T03:04:05Z").ToUniversalTime(), asset.FileCreatedAt.ToUniversalTime());
        Assert.Equal(800, asset.ExifImageWidth);
        Assert.Equal(600, asset.ExifImageHeight);
        Assert.Equal("IMAGE", asset.Type);
        Assert.Equal("image/jpeg", asset.OriginalMimeType);
    }

    [Fact]
    public void ImmichSearchResponse_Deserialize_MapsV3AlbumSearchShape()
    {
        // The exact JSON shape Immich v3's POST /api/search/metadata returns
        // when filtered by albumIds. The IndiE-API now relies on this shape
        // because v3 dropped the per-album GET that embedded `assets[]`.
        const string json = @"{
            ""assets"": {
                ""total"": 2,
                ""count"": 2,
                ""items"": [
                    { ""id"": ""a1"", ""type"": ""IMAGE"", ""fileCreatedAt"": ""2023-01-01T12:00:00Z"", ""originalMimeType"": ""image/png"", ""exifImageWidth"": 1024, ""exifImageHeight"": 768 },
                    { ""id"": ""a2"", ""type"": ""VIDEO"", ""fileCreatedAt"": ""2022-12-31T08:30:00Z"", ""originalMimeType"": ""video/mp4"", ""exifImageWidth"": 640, ""exifImageHeight"": 480 }
                ]
            }
        }";

        var deserialized = JsonSerializer.Deserialize<ImmichSearchResponse>(json);
        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized!.Assets);
        Assert.Equal(2, deserialized.Assets.Total);
        Assert.Equal(2, deserialized.Assets.Count);
        Assert.Equal(2, deserialized.Assets.Items.Count);

        Assert.Equal("a1", deserialized.Assets.Items[0].Id);
        Assert.Equal("IMAGE", deserialized.Assets.Items[0].Type);
        Assert.Equal(DateTime.Parse("2023-01-01T12:00:00Z").ToUniversalTime(), deserialized.Assets.Items[0].FileCreatedAt.ToUniversalTime());
        Assert.Equal("image/png", deserialized.Assets.Items[0].OriginalMimeType);
        Assert.Equal(1024, deserialized.Assets.Items[0].ExifImageWidth);
        Assert.Equal(768, deserialized.Assets.Items[0].ExifImageHeight);

        Assert.Equal("a2", deserialized.Assets.Items[1].Id);
        Assert.Equal("VIDEO", deserialized.Assets.Items[1].Type);
    }

    [Fact]
    public void ArtWorkResponse_RecordEquality_ShouldMatchValues()
    {
        var now = DateTime.UtcNow;
        var response = new ArtWorkResponse("id123", now, "/api/art/image/id123");
        Assert.Equal("id123", response.Id);
        Assert.Equal(now, response.Date);
        Assert.Equal("/api/art/image/id123", response.ImageUrl);
        // Equality test
        var same = new ArtWorkResponse("id123", now, "/api/art/image/id123");
        Assert.Equal(response, same);
    }
}
