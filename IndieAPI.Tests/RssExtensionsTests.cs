using IndieAPI.Api.Endpoints;
using IndieAPI.Api.Models;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IndieAPI.Tests;

public class RssExtensionsTests
{
    [Fact]
    public async Task GenerateRssFeed_ReturnsValidRssXml()
    {
        // Arrange
        var articles = new List<ArticleSummary>
        {
            new ArticleSummary 
            { 
                Title = "Test Article 1", 
                Description = "Description 1", 
                Link = "article-1", 
                Date = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc) 
            },
            new ArticleSummary 
            { 
                Title = "Test Article 2", 
                Description = "Description 2", 
                Link = "article-2", 
                Date = new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Utc) 
            }
        };

        var feedTitle = "My Web Feed";
        var feedDescription = "Feed for testing";
        var feedAlternativeUrl = "https://example.com/feed";
        Func<string, string> itemUrlBuilder = link => $"https://example.com/articles/{link}";

        // Act
        var result = articles.GenerateRssFeed(feedTitle, feedDescription, feedAlternativeUrl, itemUrlBuilder);

        // Assert
        Assert.NotNull(result);

        // To read the content from IResult, we execute it against a mock HttpContext
        var services = new ServiceCollection();
        services.AddLogging();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
        httpContext.Response.Body = new MemoryStream();

        await result.ExecuteAsync(httpContext);

        httpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(httpContext.Response.Body);
        var xmlContent = await reader.ReadToEndAsync();

        Assert.Equal("application/rss+xml", httpContext.Response.ContentType);
        
        // Feed properties
        Assert.Contains($"<title>{feedTitle}</title>", xmlContent);
        Assert.Contains($"<description>{feedDescription}</description>", xmlContent);
        Assert.Contains($"<link>{feedAlternativeUrl}</link>", xmlContent);

        // Item 1 properties
        Assert.Contains("<title>Test Article 1</title>", xmlContent);
        Assert.Contains("<description>Description 1</description>", xmlContent);
        Assert.Contains("<link>https://example.com/articles/article-1</link>", xmlContent);

        // Item 2 properties
        Assert.Contains("<title>Test Article 2</title>", xmlContent);
        Assert.Contains("<description>Description 2</description>", xmlContent);
        Assert.Contains("<link>https://example.com/articles/article-2</link>", xmlContent);
    }

    [Fact]
    public async Task GenerateRssFeed_WithEmptyArticles_ReturnsValidRssXml()
    {
        // Arrange
        var articles = new List<ArticleSummary>();

        // Act
        var result = articles.GenerateRssFeed("Empty Feed", "No articles", "https://example.com", link => $"https://example.com/item/{link}");

        // Assert
        var services = new ServiceCollection();
        services.AddLogging();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
        httpContext.Response.Body = new MemoryStream();

        await result.ExecuteAsync(httpContext);

        httpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(httpContext.Response.Body);
        var xmlContent = await reader.ReadToEndAsync();

        Assert.Contains("<title>Empty Feed</title>", xmlContent);
        Assert.DoesNotContain("<item>", xmlContent);
    }
    
    [Fact]
    public async Task GenerateRssFeed_WithItemUrlBuilder_ShouldConstructCorrectLinks()
    {
        // Arrange
        var articles = new List<ArticleSummary>
        {
            new ArticleSummary 
            { 
                Title = "Test Article", 
                Description = "Description", 
                Link = "path/to/article.md", 
                Date = DateTime.UtcNow 
            }
        };

        Func<string, string> itemUrlBuilder = link => $"https://example.com/articles/{link}";

        // Act
        var result = articles.GenerateRssFeed("Feed", "Desc", "https://example.com", itemUrlBuilder);

        // Assert
        var services = new ServiceCollection();
        services.AddLogging();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
        httpContext.Response.Body = new MemoryStream();

        await result.ExecuteAsync(httpContext);

        httpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(httpContext.Response.Body);
        var xmlContent = await reader.ReadToEndAsync();

        // Ensure there is exactly one slash between the prefix and the link
        Assert.Contains("<link>https://example.com/articles/path/to/article.md</link>", xmlContent);
    }
}
