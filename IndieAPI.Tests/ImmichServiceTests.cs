using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using IndieAPI.Api.Models;
using IndieAPI.Api.Services;
using Xunit;

namespace IndieAPI.Tests;

public class ImmichServiceTests
{
    private HttpClient CreateHttpClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new TestMessageHandler(responder);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        return client;
    }

    private class TestMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public TestMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }

    private IConfiguration BuildConfig(params string[] albumIds)
    {
        var dict = new Dictionary<string, string>();
        for (int i = 0; i < albumIds.Length; i++)
        {
            dict[$"Immich:AlbumIds:{i}"] = albumIds[i];
        }
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    [Fact]
    public async Task GetPagedArtAsync_ReturnsSortedImages_AndRespectsPagination()
    {
        // Arrange – two albums with various assets
        var album1 = new ImmichAlbumResponse
        {
            Id = "album1",
            AlbumName = "First",
            Assets = new List<ImmichAsset>
            {
                new ImmichAsset { Id = "A", Type = "IMAGE", FileCreatedAt = new DateTime(2022,1,1) },
                new ImmichAsset { Id = "V", Type = "VIDEO", FileCreatedAt = new DateTime(2023,1,1) }
            }
        };
        var album2 = new ImmichAlbumResponse
        {
            Id = "album2",
            AlbumName = "Second",
            Assets = new List<ImmichAsset>
            {
                new ImmichAsset { Id = "B", Type = "IMAGE", FileCreatedAt = new DateTime(2023,1,2) }
            }
        };

        var client = CreateHttpClient(req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.StartsWith("/api/albums/album1"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(album1) };
            if (path.StartsWith("/api/albums/album2"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(album2) };
            // default fallback
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var config = BuildConfig("album1", "album2");
        var logger = Mock.Of<ILogger<ImmichService>>();
        var service = new ImmichService(client, config);

        // Act – page 1, pageSize 10 (all results)
        var resultAll = await service.GetPagedArtAsync(1, 10);
        var listAll = new List<ArtWorkResponse>(resultAll);

        // Assert – should contain only two IMAGE assets, sorted newest first (B then A)
        Assert.Equal(2, listAll.Count);
        Assert.Equal("B", listAll[0].Id);
        Assert.Equal("A", listAll[1].Id);

        // Act – page 2, pageSize 1 (second item)
        var resultPage2 = await service.GetPagedArtAsync(2, 1);
        var listPage2 = new List<ArtWorkResponse>(resultPage2);
        Assert.Single(listPage2);
        Assert.Equal("A", listPage2[0].Id);
    }

    [Fact]
    public async Task GetImageThumbnailAsync_ReturnsOriginalForAnimatedAsset()
    {
        // Arrange animated asset metadata
        var animatedMeta = new ImmichAsset { Id = "anim", OriginalMimeType = "image/gif" };
        byte[] imgBytes = new byte[] { 1, 2, 3 };
        var client = CreateHttpClient(req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path == $"/api/assets/anim")
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(animatedMeta) };
            if (path == $"/api/assets/anim/original")
            {
                var resp = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(imgBytes) };
                resp.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/gif");
                return resp;
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        var config = BuildConfig(); // no albums needed
        var logger = Mock.Of<ILogger<ImmichService>>();
        var service = new ImmichService(client, config);

        // Act
        var result = await service.GetImageThumbnailAsync("anim");

        // Assert
        using var ms = new MemoryStream();
        await result.Stream.CopyToAsync(ms);
        Assert.Equal("image/gif", result.ContentType);
        Assert.Equal(imgBytes, ms.ToArray());
    }

    [Fact]
    public async Task GetImageThumbnailAsync_ReturnsThumbnailForStaticAsset()
    {
        var staticMeta = new ImmichAsset { Id = "static", OriginalMimeType = "image/png" };
        byte[] imgBytes = new byte[] { 9, 8, 7 };
        var client = CreateHttpClient(req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path == $"/api/assets/static")
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(staticMeta) };
            if (path == $"/api/assets/static/thumbnail") // query ignored for matching
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(imgBytes) };
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        var config = BuildConfig();
        var logger = Mock.Of<ILogger<ImmichService>>();
        var service = new ImmichService(client, config);

        var result = await service.GetImageThumbnailAsync("static");
        using var ms = new MemoryStream();
        await result.Stream.CopyToAsync(ms);
        // ContentType defaults to image/jpeg if not set, but we can accept that.
        Assert.Equal(imgBytes, ms.ToArray());
    }
}
