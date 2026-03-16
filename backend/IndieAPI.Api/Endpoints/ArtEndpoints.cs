using IndieAPI.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IndieAPI.Api.Endpoints;

public static class ArtEndpoints
{
    public static void MapArtEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/art");

        // The Metadata endpoint for infinite scroll
        group.MapGet("/", async (IImmichService immichService, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        {
            var art = await immichService.GetPagedArtAsync(page, pageSize);
            return Results.Ok(art);
        });

        // The Image Proxy endpoint (so we don't leak the Immich API Key)
        group.MapGet("/image/{id}", async (IImmichService immichService, string id) =>
{
            var imageFile = await immichService.GetImageThumbnailAsync(id);
            
            return Results.Stream(imageFile.Stream, imageFile.ContentType);
        });
    }
}