using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using IndieAPI.Api.Interfaces;
using IndieAPI.Api.Models;
using IndieAPI.Api.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace IndieAPI.Tests;

public class ArticleServiceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ArticleServiceTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProjects_ReturnsPagedData()
    {
        // Arrange
        var mockService = new Mock<IArticleService>();
        var fakeResult = new PagedArticleResult
        {
            CurrentPage = 1,
            TotalPages = 1,
            Articles = new List<ArticleSummary>
            {
                new ArticleSummary { Title = "Stoat Sync", Link = "/projects/2026/Stoat-Sync/project.md" }
            }
        };

        mockService.Setup(s => s.GetPagedProjectsAsync(1, 3)).ReturnsAsync(fakeResult);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAllKeyed<IArticleService>("projects");
                services.AddKeyedSingleton("projects", mockService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/projects?page=1&pageSize=3");

        // Assert
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<PagedArticleResult>();
        Assert.NotNull(data);
        Assert.Single(data.Articles);
        Assert.Equal("Stoat Sync", data.Articles.First().Title);
    }

    [Fact]
    public void ProcessThumbnailPath_HandlesOldAbsoluteFormat()
    {
        // Arrange
        var service = new ArticleService(null!, null!, "Projects", "projects");
        var input = "/projects/image.png";
        var expected = "/api/projects/asset/image.png";

        // Act
        var result = service.ProcessThumbnailPath(input, "folder");

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ProcessThumbnailPath_HandlesNakedFilenames()
    {
        // Arrange
        var service = new ArticleService(null!, null!, "Projects", "projects");
        var input = "image.png";
        var expected = "/api/projects/asset/test-folder/image.png";

        // Act
        var result = service.ProcessThumbnailPath(input, "test-folder");

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ProcessThumbnailPath_LeavesHttpUrlsUntouched()
    {
        // Arrange
        var service = new ArticleService(null!, null!, "Projects", "projects");
        var input = "https://example.com/image.png";

        // Act
        var result = service.ProcessThumbnailPath(input, "folder");

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void FixNakedImagePaths_TransformsRelativePaths()
    {
        // Arrange
        var service = new ArticleService(null!, null!, "Projects", "projects");
        var input = "![Alt](image.png)";
        var expected = "![Alt](/api/projects/asset/test-folder/image.png)";

        // Act
        var result = service.FixNakedImagePaths(input, "test-folder");

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FixNakedImagePaths_LeavesHttpUrlsUntouched()
    {
        // Arrange
        var service = new ArticleService(null!, null!, "Projects", "projects");
        var input = "![Alt](https://example.com/image.png)";

        // Act
        var result = service.FixNakedImagePaths(input, "folder");

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void MinifyHtmlFooter_RemovesNewlinesBetweenTags()
    {
        // Arrange
        var service = new ArticleService(null!, null!, "Projects", "projects");
        var input = "<img>\n<a>";
        var expected = "<img><a>";

        // Act
        var result = service.MinifyHtmlFooter(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ProcessMarkdownContent_IntegratesAllTransformations()
    {
        // Arrange
        var service = new ArticleService(null!, null!, "Projects", "projects");
        var input = "![Alt](image.png)\n<img>\n<a>";

        // Act
        var result = service.ProcessMarkdownContent(input, "test-folder");

        // Assert
        Assert.Contains("/api/projects/asset/test-folder/image.png", result);
        Assert.Contains("<img><a>", result);
    }
}