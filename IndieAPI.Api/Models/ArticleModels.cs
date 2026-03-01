namespace IndieAPI.Api.Models;

// The structure that matches your Markdown YAML block
public class ArticleFrontmatter
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Thumbnail { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
}

// What we return for the Project List (Infinite Scroll / Paging)
public class ArticleSummary
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Thumbnail { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Link { get; set; } = string.Empty;
}

// What we return for the actual Project Page
public class FullArticle : ArticleSummary
{
    public string Content { get; set; } = string.Empty;
}

// Paged Response Wrapper
public class PagedArticleResult
{
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public IEnumerable<ArticleSummary> Articles { get; set; } = new List<ArticleSummary>();
}