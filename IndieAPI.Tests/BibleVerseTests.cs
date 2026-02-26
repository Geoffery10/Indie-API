using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using IndieAPI.Api;

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
        // Arrange: Create a mock of our future service
        var mockHaService = new Mock<IHomeAssistantService>();
        var expectedVerse = "John 3:16 - For God so loved the world...";
        
        mockHaService
            .Setup(s => s.GetDailyVerseAsync())
            .ReturnsAsync(expectedVerse);

        // Swap out the real service for our mock in the test server
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real service
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IHomeAssistantService));
                if (descriptor != null) services.Remove(descriptor);

                // Add the mocked service
                services.AddSingleton(mockHaService.Object);
            });
        }).CreateClient();

        // Act: Make the API call
        var response = await client.GetAsync("/api/bible-daily-verse");

        // Assert: Ensure it returns 200 OK and the mocked verse
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains(expectedVerse, content);
    }
}