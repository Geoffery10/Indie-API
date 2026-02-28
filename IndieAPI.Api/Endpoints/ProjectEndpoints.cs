using IndieAPI.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IndieAPI.Api.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects");

        group.MapGet("/", async (IProjectService projectService, [FromQuery] int page = 1,[FromQuery] int pageSize = 3) =>
        {
            var result = await projectService.GetPagedProjectsAsync(page, pageSize);
            return Results.Ok(result);
        });

        group.MapGet("/article", async (IProjectService projectService, [FromQuery] string id) =>
        {
            var article = await projectService.GetArticleAsync(id);
            if (article == null) return Results.NotFound(new { Message = "Article not found." });
            
            return Results.Ok(article);
        });

        // NEW: The Asset Streamer
        // The {**path} is a catch-all route parameter that allows slashes in the URL
        group.MapGet("/asset/{**path}", (IProjectService projectService, string path) =>
        {
            return projectService.GetAsset(path);
        });
    }
}