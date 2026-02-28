using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using IndieAPI.Api.Interfaces;
using IndieAPI.Api.Models;

namespace IndieAPI.Tests;

public class ProjectTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProjectTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProjects_ReturnsPagedData()
    {
        // Arrange
        var mockService = new Mock<IProjectService>();
        var fakeResult = new PagedProjectResult
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
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IProjectService));
                if (descriptor != null) services.Remove(descriptor);
                services.AddSingleton(mockService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/projects?page=1&pageSize=3");

        // Assert
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<PagedProjectResult>();
        Assert.NotNull(data);
        Assert.Single(data.Articles);
        Assert.Equal("Stoat Sync", data.Articles.First().Title);
    }
}