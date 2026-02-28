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
            // 1. Calculate the relative folder path (e.g., "2026/Discord_to_Stoat_Migration")
            // We need this so we know where to look for the "naked" image links
            var fileDirectory = Path.GetDirectoryName(file);
            var relativeFolder = Path.GetRelativePath(contentPath, fileDirectory!)
                .Replace("\\", "/"); // Ensure forward slashes for URLs

            var fileContent = await File.ReadAllTextAsync(file);
            
            // 2. Pass the folder path to the parser
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

            // --- 1. HANDLE THUMBNAIL PATH ---
            var thumbnail = frontmatter.Thumbnail;

            if (thumbnail.StartsWith("/projects/"))
            {
                // If it's an old-style absolute path, convert it once
                thumbnail = thumbnail.Replace("/projects/", "/api/projects/asset/");
            }
            else if (!thumbnail.StartsWith("http") && !thumbnail.StartsWith("/"))
            {
                // If it's a "naked" filename, prepend the folder path
                thumbnail = $"/api/projects/asset/{relativeFolder}/{thumbnail}";
            }

            // --- 2. HANDLE MARKDOWN BODY ---
            
            // Convert .md links to .html
            var cleanMarkdown = markdownBody.Replace(".md)", ".html)");

            // REGEX: Find standard markdown images like ![Alt](image.png)
            // This pattern ignores links starting with http or /
            string pattern = @"(!\[.*?\]\()(?!(http|/))(.*?)(\))";
            
            cleanMarkdown = Regex.Replace(cleanMarkdown, pattern, m => 
            {
                var prefix = m.Groups[1].Value;  // "![Alt]("
                var filename = m.Groups[3].Value; // "image.png"
                var suffix = m.Groups[4].Value;   // ")"
                return $"{prefix}/api/projects/asset/{relativeFolder}/{filename}{suffix}";
            });

            // LEGACY CLEANUP: Only replace /projects/ if it hasn't been turned into /api/ yet
            // We do a specific check to avoid the double-append
            if (cleanMarkdown.Contains("](/projects/"))
            {
                cleanMarkdown = cleanMarkdown.Replace("](/projects/", "](/api/projects/asset/");
            }
            if (cleanMarkdown.Contains("src=\"/projects/"))
            {
                cleanMarkdown = cleanMarkdown.Replace("src=\"/projects/", "src=\"/api/projects/asset/");
            }

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