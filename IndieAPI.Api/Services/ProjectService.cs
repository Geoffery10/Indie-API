using IndieAPI.Api.Interfaces;
using IndieAPI.Api.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Markdig;
using System.Text.RegularExpressions;

namespace IndieAPI.Api.Services;

public class ProjectService : IProjectService
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    private List<FullArticle>? _cachedArticles;
    private DateTime _lastCacheUpdate = DateTime.MinValue;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(1);
    private readonly IDeserializer _yamlDeserializer;
    private readonly MarkdownPipeline _markdownPipeline;

    public ProjectService(IWebHostEnvironment env, IConfiguration config)
    {
        _env = env;
        _config = config;
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    private string GetProjectsRoot()
    {
        var customPath = _config["Projects:Directory"];
        if (!string.IsNullOrEmpty(customPath)) return customPath;
        return Path.Combine(_env.ContentRootPath, "Data", "Projects");
    }

    private async Task EnsureArticlesLoadedAsync()
    {
        if (_cachedArticles != null && (DateTime.UtcNow - _lastCacheUpdate) < _cacheDuration) return;

        var newArticleList = new List<FullArticle>();
        var contentPath = GetProjectsRoot();

        if (!Directory.Exists(contentPath)) return;

        var files = Directory.GetFiles(contentPath, "*.md", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var fileDirectory = Path.GetDirectoryName(file);
            var relativeFolder = Path.GetRelativePath(contentPath, fileDirectory!)
                .Replace("\\", "/"); 

            var fileContent = await File.ReadAllTextAsync(file);
            var article = ParseMarkdownFile(fileContent, relativeFolder);
            
            if (article != null) newArticleList.Add(article);
        }

        _cachedArticles = newArticleList.OrderByDescending(a => a.Date).ToList();
        _lastCacheUpdate = DateTime.UtcNow;
    }

    private FullArticle? ParseMarkdownFile(string fileContent, string relativeFolder)
    {
        var parts = fileContent.Split("---", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return null;

        try
        {
            var yamlContent = parts[0];
            var markdownBody = parts[1].Trim();
            var frontmatter = _yamlDeserializer.Deserialize<ArticleFrontmatter>(yamlContent);

            // --- 1. HANDLE THUMBNAIL PATH (Strict Fix) ---
            var thumbnail = frontmatter.Thumbnail;

            // Check specifically for the old absolute format
            if (thumbnail.StartsWith("/projects/"))
            {
                thumbnail = thumbnail.Replace("/projects/", "/api/projects/asset/");
            }
            // Check specifically for "naked" filenames (no slash, no http)
            else if (!thumbnail.Contains("/") && !thumbnail.StartsWith("http"))
            {
                thumbnail = $"/api/projects/asset/{relativeFolder}/{thumbnail}";
            }
            // (If it already starts with /api/ or http, we leave it alone)

            // --- 2. HANDLE MARKDOWN BODY ---
            
            var cleanMarkdown = markdownBody.Replace(".md)", ".html)");

            // REGEX 1: Fix Naked Images in Markdown
            // Transforms ![Alt](img.png) -> ![Alt](/api/projects/asset/Folder/img.png)
            string imgPattern = @"(!\[.*?\]\()(?!(http|/))(.*?)(\))";
            cleanMarkdown = Regex.Replace(cleanMarkdown, imgPattern, m => 
            {
                var prefix = m.Groups[1].Value;
                var filename = m.Groups[3].Value;
                var suffix = m.Groups[4].Value;
                return $"{prefix}/api/projects/asset/{relativeFolder}/{filename}{suffix}";
            });

            // REGEX 2: Minify HTML Footer (The Newline Fix)
            // Finds any newline that sits between a closing tag > and an opening tag <
            // Example: "<img>\n<a>" becomes "<img><a>"
            string footerPattern = @"(?<=>)\s*\n\s*(?=<)";
            cleanMarkdown = Regex.Replace(cleanMarkdown, footerPattern, "");

            // Legacy cleanup (Only runs if the string explicitly contains the old path)
            if (cleanMarkdown.Contains("](/projects/"))
                cleanMarkdown = cleanMarkdown.Replace("](/projects/", "](/api/projects/asset/");
            if (cleanMarkdown.Contains("src=\"/projects/"))
                cleanMarkdown = cleanMarkdown.Replace("src=\"/projects/", "src=\"/api/projects/asset/");

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
        catch { return null; }
    }

    public async Task<PagedProjectResult> GetPagedProjectsAsync(int page, int pageSize)
    {
        await EnsureArticlesLoadedAsync();
        var totalArticles = _cachedArticles!.Count;
        var totalPages = (int)Math.Ceiling((double)totalArticles / pageSize);
        var pagedData = _cachedArticles!.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new ArticleSummary { Title = a.Title, Description = a.Description, Thumbnail = a.Thumbnail, Date = a.Date, Link = a.Link });
        return new PagedProjectResult { CurrentPage = page, TotalPages = totalPages, Articles = pagedData };
    }

    public async Task<FullArticle?> GetArticleAsync(string linkId)
    {
        await EnsureArticlesLoadedAsync();
        return _cachedArticles!.FirstOrDefault(a => a.Link.Equals(linkId, StringComparison.OrdinalIgnoreCase));
    }

    public IResult GetAsset(string path)
    {
        var rootDir = GetProjectsRoot();
        var physicalPath = Path.Combine(rootDir, path);
        
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