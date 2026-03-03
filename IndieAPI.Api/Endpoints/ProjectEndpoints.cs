using IndieAPI.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IndieAPI.Api.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects");

        group.MapGet("/", async ([FromKeyedServices("projects")] IArticleService projectService, [FromQuery] int page = 1,[FromQuery] int pageSize = 3) =>
        {
            var result = await projectService.GetPagedProjectsAsync(page, pageSize);
            return Results.Ok(result);
        });

        group.MapGet("/article", async ([FromKeyedServices("projects")] IArticleService projectService, [FromQuery] string id) =>
        {
            var article = await projectService.GetArticleAsync(id);
            if (article == null) return Results.NotFound(new { Message = "Article not found." });
            
            return Results.Ok(article);
        });

        // NEW: The Asset Streamer
        // The {**path} is a catch-all route parameter that allows slashes in the URL
        group.MapGet("/asset/{**path}", ([FromKeyedServices("projects")] IArticleService projectService, string path) =>
        {
            return projectService.GetAsset(path);
        });

        group.MapGet("/rss", async ([FromKeyedServices("projects")] IArticleService projectService) =>
        {
            var result = await projectService.GetPagedProjectsAsync(1, 20);
            return result.Articles.GenerateRssFeed(
                "Geoffery10 Projects",
                "Latest projects from Geoffery10",
                "https://indie.geoffery10.com/projects",
                link => $"https://indie.geoffery10.com/view-project.html?id={Uri.EscapeDataString(link)}"
            );
        });
    }
}