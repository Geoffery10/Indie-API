using IndieAPI.Api.Interfaces;
using IndieAPI.Api.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Markdig;

namespace IndieAPI.Api.Services;

public class ProjectService : IProjectService
{
    private readonly IWebHostEnvironment _env;
    private List<FullArticle>? _cachedArticles;
    private readonly IDeserializer _yamlDeserializer;
    private readonly MarkdownPipeline _markdownPipeline;

    public ProjectService(IWebHostEnvironment env)
    {
        _env = env;
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        // Configures Markdig to support tables, embedded HTML, auto-links, etc.
        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    private async Task EnsureArticlesLoadedAsync()
    {
        if (_cachedArticles != null) return;

        _cachedArticles = new List<FullArticle>();

        var contentPath = Path.Combine(_env.ContentRootPath, "Data", "Projects");
        if (!Directory.Exists(contentPath)) Directory.CreateDirectory(contentPath);

        var files = Directory.GetFiles(contentPath, "*.md", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var fileContent = await File.ReadAllTextAsync(file);
            var article = ParseMarkdownFile(fileContent);
            if (article != null) _cachedArticles.Add(article);
        }

        _cachedArticles = _cachedArticles.OrderByDescending(a => a.Date).ToList();
    }

    private FullArticle? ParseMarkdownFile(string fileContent)
    {
        var parts = fileContent.Split("---", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return null;

        try
        {
            var yamlContent = parts[0];
            var markdownBody = parts[1].Trim();

            var frontmatter = _yamlDeserializer.Deserialize<ArticleFrontmatter>(yamlContent);

            // 1. Convert .md links to .html links for your frontend routing
            var cleanMarkdown = markdownBody.Replace(".md)", ".html)");

            // 2. Route Markdown image paths to our new API asset endpoint
            // E.g., ](/projects/2026...) -> ](/api/projects/asset/2026...)
            cleanMarkdown = cleanMarkdown.Replace("](/projects/", "](/api/projects/asset/");
            cleanMarkdown = cleanMarkdown.Replace("src=\"/projects/", "src=\"/api/projects/asset/");
            
            var thumbnail = frontmatter.Thumbnail.Replace("/projects/", "/api/projects/asset/");

            // 3. Convert the Markdown to HTML!
            var htmlContent = Markdown.ToHtml(cleanMarkdown, _markdownPipeline);

            return new FullArticle
            {
                Title = frontmatter.Title,
                Description = frontmatter.Description,
                Thumbnail = thumbnail,
                Date = DateTime.TryParse(frontmatter.Date, out var parsedDate) ? parsedDate : DateTime.MinValue,
                Link = frontmatter.Link,
                Content = htmlContent
            };
        }
        catch
        {
            return null; 
        }
    }

    public async Task<PagedProjectResult> GetPagedProjectsAsync(int page, int pageSize)
    {
        await EnsureArticlesLoadedAsync();

        var totalArticles = _cachedArticles!.Count;
        var totalPages = (int)Math.Ceiling((double)totalArticles / pageSize);

        var pagedData = _cachedArticles!
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ArticleSummary 
            {
                Title = a.Title,
                Description = a.Description,
                Thumbnail = a.Thumbnail,
                Date = a.Date,
                Link = a.Link
            });

        return new PagedProjectResult
        {
            CurrentPage = page,
            TotalPages = totalPages,
            Articles = pagedData
        };
    }

    public async Task<FullArticle?> GetArticleAsync(string linkId)
    {
        await EnsureArticlesLoadedAsync();
        return _cachedArticles!.FirstOrDefault(a => a.Link.Equals(linkId, StringComparison.OrdinalIgnoreCase));
    }

    // NEW: Streams the image files directly from the disk
    public IResult GetAsset(string path)
    {
        // path will be something like "2026/Stoat-Sync/stoat-sync-profile.png"
        var physicalPath = Path.Combine(_env.ContentRootPath, "data", "projects", path);
        
        if (!File.Exists(physicalPath)) return Results.NotFound();

        var ext = Path.GetExtension(physicalPath).ToLowerInvariant();
        var mimeType = ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };

        return Results.File(physicalPath, mimeType);
    }
}