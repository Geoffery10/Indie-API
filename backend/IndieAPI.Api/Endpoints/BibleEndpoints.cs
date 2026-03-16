using IndieAPI.Api.Interfaces;

namespace IndieAPI.Api.Endpoints;

public static class BibleEndpoints
{
    public static void MapBibleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/bible-daily-verse", GetDailyVerse);
    }

    private static async Task<IResult> GetDailyVerse(IHomeAssistantService haService)
    {
        var verse = await haService.GetDailyVerseAsync();
        return Results.Ok(new { Verse = verse });
    }
}