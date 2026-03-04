using IndieAPI.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IndieAPI.Api.Endpoints;

public static class ArticleEndpoints
{
    public static void MapArticleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/articles/rss", async (
            [FromKeyedServices("projects")] IArticleService projectService,
            [FromKeyedServices("blogs")] IArticleService blogService) =>
        {
            var projectsResult = await projectService.GetPagedProjectsAsync(1, 25);
            var blogsResult = await blogService.GetPagedProjectsAsync(1, 25);

            var allArticles = projectsResult.Articles
                .Concat(blogsResult.Articles)
                .OrderByDescending(a => a.Date)
                .Take(25);

            return allArticles.GenerateRssFeed(
                "Geoffery10 Articles",
                "Latest projects and blog posts from Geoffery10",
                "https://indie.geoffery10.com",
                article => article.Category == "projects" 
                    ? $"https://indie.geoffery10.com/view-project.html?id={Uri.EscapeDataString(article.Link)}"
                    : $"https://indie.geoffery10.com/view-blog.html?id={Uri.EscapeDataString(article.Link)}"
            );
        }).RequireCors("AllowAnyOrigin");
    }
}
