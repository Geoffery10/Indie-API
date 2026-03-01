using IndieAPI.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IndieAPI.Api.Endpoints;

public static class BlogEndpoints
{
    public static void MapBlogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/blogs");

        group.MapGet("/", async ([FromKeyedServices("blogs")] IArticleService blogService, [FromQuery] int page = 1,[FromQuery] int pageSize = 3) =>
        {
            var result = await blogService.GetPagedProjectsAsync(page, pageSize);
            return Results.Ok(result);
        });

        group.MapGet("/article", async ([FromKeyedServices("blogs")] IArticleService blogService, [FromQuery] string id) =>
        {
            var article = await blogService.GetArticleAsync(id);
            if (article == null) return Results.NotFound(new { Message = "Article not found." });
            
            return Results.Ok(article);
        });

        group.MapGet("/asset/{**path}", ([FromKeyedServices("blogs")] IArticleService blogService, string path) =>
        {
            return blogService.GetAsset(path);
        });
    }
}
