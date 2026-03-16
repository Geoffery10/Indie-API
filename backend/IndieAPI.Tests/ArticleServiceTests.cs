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

    [Fact]
    public async Task GetCombinedRssFeed_ReturnsValidXml()
    {
        // Arrange
        var mockProjectService = new Mock<IArticleService>();
        var projectArticles = new PagedArticleResult
        {
            CurrentPage = 1,
            TotalPages = 1,
            Articles = new List<ArticleSummary>
            {
                new ArticleSummary { Title = "Project 1", Link = "p1", Category = "projects", Date = DateTime.UtcNow }
            }
        };

        var mockBlogService = new Mock<IArticleService>();
        var blogArticles = new PagedArticleResult
        {
            CurrentPage = 1,
            TotalPages = 1,
            Articles = new List<ArticleSummary>
            {
                new ArticleSummary { Title = "Blog 1", Link = "b1", Category = "blogs", Date = DateTime.UtcNow.AddHours(-1) }
            }
        };

        mockProjectService.Setup(s => s.GetPagedProjectsAsync(1, 25)).ReturnsAsync(projectArticles);
        mockBlogService.Setup(s => s.GetPagedProjectsAsync(1, 25)).ReturnsAsync(blogArticles);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAllKeyed<IArticleService>("projects");
                services.AddKeyedSingleton("projects", mockProjectService.Object);
                services.RemoveAllKeyed<IArticleService>("blogs");
                services.AddKeyedSingleton("blogs", mockBlogService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/articles/rss");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/rss+xml", response.Content.Headers.ContentType?.MediaType);
        var xmlString = await response.Content.ReadAsStringAsync();
        Assert.Contains("<title>Project 1</title>", xmlString);
        Assert.Contains("<title>Blog 1</title>", xmlString);
        Assert.Contains("view-project.html?id=p1", xmlString);
        Assert.Contains("view-blog.html?id=b1", xmlString);
    }

    [Fact]
    public async Task OldRssEndpoints_ReturnNotFound()
    {
        // Act
        var client = _factory.CreateClient();
        var projectsResponse = await client.GetAsync("/api/projects/rss");
        var blogsResponse = await client.GetAsync("/api/blogs/rss");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, projectsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, blogsResponse.StatusCode);
    }
}