using System.Collections.Generic;
using System.Text.Json;
using IndieAPI.Api.Models;
using Xunit;

namespace IndieAPI.Tests;

public class HaStateResponseTests
{
    [Fact]
    public void Deserialize_WithStateAndAttributes_ShouldPopulateProperties()
    {
        var json = "{\"state\":\"online\",\"attributes\":{\"text\":\"Hello\",\"value\":42}}";
        var result = JsonSerializer.Deserialize<HaStateResponse>(json);
        Assert.NotNull(result);
        Assert.Equal("online", result!.State);
        Assert.NotNull(result.Attributes);
        Assert.Equal("Hello", result.Attributes!["text"].ToString());
        // The numeric value is deserialized as JsonElement; extract as int
        var valueObj = result.Attributes!["value"];
        int value;
        if (valueObj is JsonElement je)
            value = je.GetInt32();
        else
            value = Convert.ToInt32(valueObj);
        Assert.Equal(42, value);
    }

    [Fact]
    public void SerializeAndDeserialize_ShouldMaintainDataIntegrity()
    {
        var original = new HaStateResponse
        {
            State = "offline",
            Attributes = new Dictionary<string, object>
            {
                { "message", "No data" },
                { "count", 5 }
            }
        };
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<HaStateResponse>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(original.State, deserialized!.State);
        Assert.Equal(original.Attributes!.Count, deserialized.Attributes!.Count);
        Assert.Equal(original.Attributes["message"].ToString(), deserialized.Attributes["message"].ToString());
        // "count" will be JsonElement after deserialization
        var countObj = deserialized.Attributes["count"];
        int count;
        if (countObj is JsonElement je)
            count = je.GetInt32();
        else
            count = Convert.ToInt32(countObj);
        Assert.Equal(5, count);
    }

    [Fact]
    public void Deserialize_WithoutAttributes_ShouldResultInNullAttributes()
    {
        var json = "{\"state\":\"unknown\"}";
        var result = JsonSerializer.Deserialize<HaStateResponse>(json);
        Assert.NotNull(result);
        Assert.Equal("unknown", result!.State);
        Assert.Null(result.Attributes);
    }
}
