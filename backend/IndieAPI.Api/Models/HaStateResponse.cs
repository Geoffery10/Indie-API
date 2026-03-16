using System.Text.Json.Serialization;

namespace IndieAPI.Api.Models;

public class HaStateResponse
{
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("attributes")]
    public Dictionary<string, object>? Attributes { get; set; }
}