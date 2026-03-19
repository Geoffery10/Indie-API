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
        var json = "{\"Id\":\"asset123\",\"FileCreatedAt\":\"2023-01-02T03:04:05Z\",\"ExifImageWidth\":800,\"ExifImageHeight\":600,\"Type\":\"IMAGE\",\"OriginalMimeType\":\"image/jpeg\"}";
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
    public void ImmichAlbumResponse_SerializeDeserialize_RoundTripPreservesData()
    {
        var album = new ImmichAlbumResponse
        {
            Id = "album1",
            AlbumName = "Test Album",
            Assets = new List<ImmichAsset>
            {
                new ImmichAsset
                {
                    Id = "a1",
                    FileCreatedAt = new DateTime(2023,01,01,12,0,0, DateTimeKind.Utc),
                    ExifImageWidth = 1024,
                    ExifImageHeight = 768,
                    Type = "IMAGE",
                    OriginalMimeType = "image/png"
                },
                new ImmichAsset
                {
                    Id = "a2",
                    FileCreatedAt = new DateTime(2022,12,31,8,30,0, DateTimeKind.Utc),
                    ExifImageWidth = 640,
                    ExifImageHeight = 480,
                    Type = "VIDEO",
                    OriginalMimeType = "video/mp4"
                }
            }
        };

        var json = JsonSerializer.Serialize(album);
        var deserialized = JsonSerializer.Deserialize<ImmichAlbumResponse>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(album.Id, deserialized!.Id);
        Assert.Equal(album.AlbumName, deserialized.AlbumName);
        Assert.Equal(album.Assets.Count, deserialized.Assets.Count);
        // Verify first asset fields
        var originalFirst = album.Assets[0];
        var roundFirst = deserialized.Assets[0];
        Assert.Equal(originalFirst.Id, roundFirst.Id);
        Assert.Equal(originalFirst.FileCreatedAt, roundFirst.FileCreatedAt);
        Assert.Equal(originalFirst.ExifImageWidth, roundFirst.ExifImageWidth);
        Assert.Equal(originalFirst.ExifImageHeight, roundFirst.ExifImageHeight);
        Assert.Equal(originalFirst.Type, roundFirst.Type);
        Assert.Equal(originalFirst.OriginalMimeType, roundFirst.OriginalMimeType);
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
