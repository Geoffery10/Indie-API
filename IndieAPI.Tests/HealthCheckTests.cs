using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IndieAPI.Tests;

// IClassFixture sets up the in-memory test server
public class HealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthCheckTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealthCheck_ReturnsOk_AndHealthyStatus()
    {
        // Arrange: Create an HTTP client pointed at our in-memory API
        var client = _factory.CreateClient();

        // Act: Make a GET request to the health endpoint
        var response = await client.GetAsync("/api/health");

        // Assert: Ensure the API returned a 200 OK
        response.EnsureSuccessStatusCode(); 
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Assert: Ensure the response body contains our expected data
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
        Assert.Contains("IndieAPI", content);
    }
}