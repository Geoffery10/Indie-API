using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using IndieAPI.Api.Interfaces;
using IndieAPI.Api.Models;

namespace IndieAPI.Tests;

public class ArtTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ArtTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetImage_ReturnsCorrectStreamAndType()
    {
        // Arrange
        var mockImmich = new Mock<IImmichService>();
        var ms = new MemoryStream(); // Fake image data
        var expectedType = "image/png";

        mockImmich
            .Setup(s => s.GetImageThumbnailAsync("test-id"))
            .ReturnsAsync(new ImageFile(ms, expectedType));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IImmichService));
                if (descriptor != null) services.Remove(descriptor);
                services.AddSingleton(mockImmich.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/art/image/test-id");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedType, response.Content.Headers.ContentType?.MediaType);
    }
}