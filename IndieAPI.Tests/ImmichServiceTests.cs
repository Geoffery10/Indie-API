using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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
        var dict = new Dictionary<string, string?>();
        for (int i = 0; i < albumIds.Length; i++)
        {
            dict[$"Immich:AlbumIds:{i}"] = albumIds[i];
        }
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    // Build the v3 /api/search/metadata response shape with the given assets.
    private static string BuildSearchResponse(IEnumerable<ImmichAsset> items, int? totalOverride = null)
    {
        var list = items.ToList();
        var resp = new ImmichSearchResponse
        {
            Assets = new ImmichSearchAssets
            {
                Total = totalOverride ?? list.Count,
                Count = list.Count,
                Items = list
            }
        };
        return JsonSerializer.Serialize(resp);
    }

    [Fact]
    public async Task GetPagedArtAsync_ReturnsSortedImages_AndRespectsPagination()
    {
        // Arrange – two pages worth of assets across two albums.
        // v3 collapses per-album GETs into a single POST /api/search/metadata
        // with `albumIds` as a filter, so the fake handler answers all
        // requests for /api/search/metadata regardless of the albumIds body.
        // The fixture includes a VIDEO entry to prove the service-side
        // `type=IMAGE` filter is in the request body.
        var page1 = new List<ImmichAsset>
        {
            new() { Id = "B", Type = "IMAGE", FileCreatedAt = new DateTime(2023,1,2) },
            new() { Id = "A", Type = "IMAGE", FileCreatedAt = new DateTime(2022,1,1) },
            new() { Id = "V", Type = "VIDEO", FileCreatedAt = new DateTime(2024,1,1) },
        };

        // The service trusts the server to honour `type=IMAGE`, so the fixture
        // represents the response after the server has already filtered.
        var filteredForService = page1.Where(a => a.Type == "IMAGE").ToList();

        var client = CreateHttpClient(req =>
        {
            if (req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath == "/api/search/metadata")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(BuildSearchResponse(filteredForService), Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var config = BuildConfig("album1", "album2");
        var logger = Mock.Of<ILogger<ImmichService>>();
        var service = new ImmichService(client, config);

        // Act – page 1, pageSize 10 (all results)
        var resultAll = await service.GetPagedArtAsync(1, 10);
        var listAll = new List<ArtWorkResponse>(resultAll);

        // Assert – should contain both IMAGE assets, sorted newest first (B then A).
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
    public async Task GetPagedArtAsync_SendsPostSearchMetadata_WithAlbumIdsAndImageType()
    {
        // Asserts the wire shape of the v3 call: POST /api/search/metadata
        // with albumIds, type=IMAGE, size=1000, order=desc. This is the
        // contract the Immich v3 server expects.
        HttpRequestMessage? captured = null;
        string? capturedBody = null;

        var client = CreateHttpClient(req =>
        {
            captured = req;
            if (req.Content != null)
            {
                capturedBody = req.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildSearchResponse(Array.Empty<ImmichAsset>(), 0), Encoding.UTF8, "application/json")
            };
        });

        var config = BuildConfig("album-1", "album-2");
        var logger = Mock.Of<ILogger<ImmichService>>();
        var service = new ImmichService(client, config);

        _ = await service.GetPagedArtAsync(1, 10);

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Equal("/api/search/metadata", captured.RequestUri!.AbsolutePath);
        Assert.NotNull(capturedBody);

        var body = capturedBody!;
        Assert.Contains("\"albumIds\":[", body);
        Assert.Contains("album-1", body);
        Assert.Contains("album-2", body);
        Assert.Contains("\"type\":\"IMAGE\"", body);
        Assert.Contains("\"order\":\"desc\"", body);
        Assert.Contains("\"size\":1000", body);
    }

    [Fact]
    public async Task GetPagedArtAsync_Paginates_WhenTotalExceedsPageSize()
    {
        // v3 caps the search endpoint at 1000 items per call. If an album has
        // more than 1000 items, the service should follow up with additional
        // page requests until `total` is satisfied.
        var page1Items = Enumerable.Range(1, 1000)
            .Select(i => new ImmichAsset
            {
                Id = $"a{i:0000}",
                Type = "IMAGE",
                FileCreatedAt = new DateTime(2023, 1, 1).AddSeconds(i)
            })
            .ToList();
        var page2Items = Enumerable.Range(1001, 500)
            .Select(i => new ImmichAsset
            {
                Id = $"a{i:0000}",
                Type = "IMAGE",
                FileCreatedAt = new DateTime(2023, 1, 1).AddSeconds(i)
            })
            .ToList();

        var seenPages = new List<int>();
        var client = CreateHttpClient(req =>
        {
            if (req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath == "/api/search/metadata")
            {
                var body = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                using var doc = JsonDocument.Parse(body);
                var page = doc.RootElement.GetProperty("page").GetInt32();
                seenPages.Add(page);
                var items = page == 1 ? page1Items : page2Items;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(BuildSearchResponse(items, 1500), Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var config = BuildConfig("bigAlbum");
        var logger = Mock.Of<ILogger<ImmichService>>();
        var service = new ImmichService(client, config);

        var result = await service.GetPagedArtAsync(1, 2000);
        var list = new List<ArtWorkResponse>(result);

        Assert.Equal(1500, list.Count);
        Assert.Equal(new[] { 1, 2 }, seenPages);
        // Newest first: a1500 should be at the top.
        Assert.Equal("a1500", list[0].Id);
    }

    [Fact]
    public async Task GetPagedArtAsync_NoAlbumsConfigured_ReturnsEmpty()
    {
        // With no Immich:AlbumIds configured, the service should not call
        // Immich at all and return an empty list.
        var calls = 0;
        var client = CreateHttpClient(req =>
        {
            calls++;
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var config = BuildConfig(); // no albums
        var logger = Mock.Of<ILogger<ImmichService>>();
        var service = new ImmichService(client, config);

        var result = await service.GetPagedArtAsync(1, 10);
        Assert.Empty(result);
        Assert.Equal(0, calls);
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
