using IndieAPI.Api.Interfaces;
using IndieAPI.Api.Models;

namespace IndieAPI.Api.Services;

public class HomeAssistantService : IHomeAssistantService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HomeAssistantService> _logger;

    public HomeAssistantService(HttpClient httpClient, ILogger<HomeAssistantService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> GetDailyVerseAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/states/sensor.daily_bible_verse");
            response.EnsureSuccessStatusCode();

            var haData = await response.Content.ReadFromJsonAsync<HaStateResponse>();

            if (haData == null) return "Verse unavailable.";

            if (haData.Attributes != null && haData.Attributes.TryGetValue("text", out var fullText))
            {
                return fullText.ToString() ?? haData.State;
            }

            return haData.State;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch daily verse.");
            return "Could not retrieve the daily verse.";
        }
    }
}