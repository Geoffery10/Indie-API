using IndieAPI.Api.Interfaces;
using IndieAPI.Api.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Markdig;
using System.Text.RegularExpressions;

namespace IndieAPI.Api.Services;

public class ArticleService : IArticleService
{
    private readonly string _contentDirectory;
    private readonly string _routePrefix;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    private List<FullArticle>? _cachedArticles;
    private DateTime _lastCacheUpdate = DateTime.MinValue;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(1);
    private readonly IDeserializer _yamlDeserializer;
    private readonly MarkdownPipeline _markdownPipeline;

    public ArticleService(IWebHostEnvironment env, IConfiguration config, string contentDirectory, string routePrefix)
    {
        _contentDirectory = contentDirectory;
        _routePrefix = routePrefix;
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

    private string GetArticlesRoot()
    {
        var customPath = _config[$"{_contentDirectory}:Directory"];
        if (!string.IsNullOrEmpty(customPath)) return customPath;
        return Path.Combine(_env.ContentRootPath, "Data", _contentDirectory);
    }

    private async Task EnsureArticlesLoadedAsync()
    {
        if (_cachedArticles != null && (DateTime.UtcNow - _lastCacheUpdate) < _cacheDuration) return;

        var newArticleList = new List<FullArticle>();
        var contentPath = GetArticlesRoot();

        if (!Directory.Exists(contentPath)) 
        {
            _cachedArticles = newArticleList;
            _lastCacheUpdate = DateTime.UtcNow;
            return;
        }

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

            var thumbnail = ProcessThumbnailPath(frontmatter.Thumbnail, relativeFolder);
            var htmlContent = ProcessMarkdownContent(markdownBody, relativeFolder);

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

    internal string ProcessThumbnailPath(string thumbnail, string relativeFolder)
    {
        // Check specifically for the old absolute format
        if (thumbnail.StartsWith($"/{_routePrefix}/"))
        {
            return thumbnail.Replace($"/{_routePrefix}/", $"/api/{_routePrefix}/asset/");
        }
        // Check specifically for "naked" filenames (no slash, no http)
        else if (!thumbnail.Contains("/") && !thumbnail.StartsWith("http"))
        {
            return $"/api/{_routePrefix}/asset/{relativeFolder}/{thumbnail}";
        }
        // (If it already starts with /api/ or http, we leave it alone)
        return thumbnail;
    }

    internal string ProcessMarkdownContent(string markdownBody, string relativeFolder)
    {
        // Initial cleanup
        var cleanMarkdown = markdownBody.Replace(".md)", ".html)");

        // Fix naked images in markdown
        cleanMarkdown = FixNakedImagePaths(cleanMarkdown, relativeFolder);

        // Minify HTML footer
        cleanMarkdown = MinifyHtmlFooter(cleanMarkdown);

        return Markdown.ToHtml(cleanMarkdown, _markdownPipeline);
    }

    internal string FixNakedImagePaths(string markdown, string relativeFolder)
    {
        string imgPattern = @"(!\[.*?\]\()(?!(http|/))(.*?)(\))";
        return Regex.Replace(markdown, imgPattern, m =>
        {
            var prefix = m.Groups[1].Value;
            var filename = m.Groups[3].Value;
            var suffix = m.Groups[4].Value;
            return $"{prefix}/api/{_routePrefix}/asset/{relativeFolder}/{filename}{suffix}";
        });
    }

    internal string MinifyHtmlFooter(string markdown)
    {
        string footerPattern = @"(?<=>)\s*\n\s*(?=<)";
        return Regex.Replace(markdown, footerPattern, "");
    }

    public async Task<PagedArticleResult> GetPagedProjectsAsync(int page, int pageSize)
    {
        await EnsureArticlesLoadedAsync();
        var totalArticles = _cachedArticles!.Count;
        var totalPages = (int)Math.Ceiling((double)totalArticles / pageSize);
        var pagedData = _cachedArticles!.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new ArticleSummary { Title = a.Title, Description = a.Description, Thumbnail = a.Thumbnail, Date = a.Date, Link = a.Link });
        return new PagedArticleResult { CurrentPage = page, TotalPages = totalPages, Articles = pagedData };
    }

    public async Task<FullArticle?> GetArticleAsync(string linkId)
    {
        await EnsureArticlesLoadedAsync();
        return _cachedArticles!.FirstOrDefault(a => a.Link.Equals(linkId, StringComparison.OrdinalIgnoreCase));
    }

    public IResult GetAsset(string path)
    {
        var rootDir = GetArticlesRoot();
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