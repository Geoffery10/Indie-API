using System.Text.Json.Serialization;

namespace IndieAPI.Api;

// 1. The Interface
public interface IHomeAssistantService
{
    Task<string> GetDailyVerseAsync();
}

// 2. The Model to parse Home Assistant's JSON response
public class HaStateResponse
{
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("attributes")]
    public Dictionary<string, object>? Attributes { get; set; }
}

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
            // Call the Home Assistant REST API for your specific sensor
            var response = await _httpClient.GetAsync("/api/states/sensor.daily_bible_verse");
            response.EnsureSuccessStatusCode();

            var haData = await response.Content.ReadFromJsonAsync<HaStateResponse>();

            if (haData == null) return "Verse unavailable.";

            // HA limits states to 255 chars. If the sensor stores the full text in an attribute, 
            // check there first. Replace "text" with whatever your sensor attribute is called (if any).
            if (haData.Attributes != null && haData.Attributes.TryGetValue("text", out var fullText))
            {
                return fullText.ToString() ?? haData.State;
            }

            return haData.State;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch daily verse from Home Assistant.");
            return "Could not retrieve the daily verse at this time.";
        }
    }
}