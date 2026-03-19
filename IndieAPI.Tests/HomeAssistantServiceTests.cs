using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Threading;
using IndieAPI.Api.Models;
using IndieAPI.Api.Services;
using Xunit;

namespace IndieAPI.Tests;

public class HomeAssistantServiceTests
{
    private HttpClient CreateHttpClient(HttpResponseMessage response)
    {
        var handler = new TestMessageHandler(response);
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };
        return client;
    }

    private class TestMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public TestMessageHandler(HttpResponseMessage response) => _response = response;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(_response);
    }

    [Fact]
    public async Task GetDailyVerse_ReturnsTextAttribute_WhenPresent()
    {
        // Arrange
        var haResponse = new HaStateResponse
        {
            State = "fallback",
            Attributes = new Dictionary<string, object> { { "text", "In the beginning..." } }
        };
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(haResponse)
        };
        var client = CreateHttpClient(httpResponse);
        var logger = Mock.Of<ILogger<HomeAssistantService>>();
        var service = new HomeAssistantService(client, logger);

        // Act
        var result = await service.GetDailyVerseAsync();

        // Assert
        Assert.Equal("In the beginning...", result);
    }

    [Fact]
    public async Task GetDailyVerse_ReturnsState_WhenTextAttributeMissing()
    {
        // Arrange
        var haResponse = new HaStateResponse
        {
            State = "Verse from state",
            Attributes = new Dictionary<string, object> { { "other", "value" } }
        };
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(haResponse)
        };
        var client = CreateHttpClient(httpResponse);
        var logger = Mock.Of<ILogger<HomeAssistantService>>();
        var service = new HomeAssistantService(client, logger);

        var result = await service.GetDailyVerseAsync();
        Assert.Equal("Verse from state", result);
    }

    [Fact]
    public async Task GetDailyVerse_ReturnsUnavailable_WhenResponseBodyIsNull()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json")
        };
        var client = CreateHttpClient(httpResponse);
        var logger = Mock.Of<ILogger<HomeAssistantService>>();
        var service = new HomeAssistantService(client, logger);

        var result = await service.GetDailyVerseAsync();
        Assert.Equal("Verse unavailable.", result);
    }

    [Fact]
    public async Task GetDailyVerse_ReturnsErrorMessage_WhenRequestFails()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var client = CreateHttpClient(httpResponse);
        var logger = Mock.Of<ILogger<HomeAssistantService>>();
        var service = new HomeAssistantService(client, logger);

        var result = await service.GetDailyVerseAsync();
        Assert.Equal("Could not retrieve the daily verse.", result);
    }
}
