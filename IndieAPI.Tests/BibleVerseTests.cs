using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using IndieAPI.Api.Interfaces;

namespace IndieAPI.Tests;

public class BibleVerseTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BibleVerseTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDailyBibleVerse_ReturnsOk_WithVerse()
    {
        var mockHaService = new Mock<IHomeAssistantService>();
        var expectedVerse = "John 3:16 - For God so loved the world...";
        
        mockHaService
            .Setup(s => s.GetDailyVerseAsync())
            .ReturnsAsync(expectedVerse);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IHomeAssistantService));
                if (descriptor != null) services.Remove(descriptor);

                services.AddSingleton(mockHaService.Object);
            });
        }).CreateClient();

        var response = await client.GetAsync("/api/bible-daily-verse");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains(expectedVerse, content);
    }
}