using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using IndieAPI.Api.Models;
using Microsoft.AspNetCore.Http;

namespace IndieAPI.Api.Endpoints;

public static class RssExtensions
{
    public static IResult GenerateRssFeed(this IEnumerable<ArticleSummary> articles, string feedTitle, string feedDescription, string feedAlternativeUrl, Func<string, string> itemUrlBuilder)
    {
        var feed = new SyndicationFeed(feedTitle, feedDescription, new Uri(feedAlternativeUrl))
        {
            Language = "en-us"
        };
        
        var items = new List<SyndicationItem>();
        
        foreach (var article in articles)
        {
            var itemUrl = itemUrlBuilder(article.Link);
            var item = new SyndicationItem(
                article.Title,
                article.Description,
                new Uri(itemUrl),
                article.Link,
                new DateTimeOffset(article.Date)
            );
            items.Add(item);
        }
        
        feed.Items = items;
        
        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            NewLineHandling = NewLineHandling.Entitize,
            NewLineOnAttributes = true,
            Indent = true
        };
        
        using var stream = new MemoryStream();
        using (var writer = XmlWriter.Create(stream, settings))
        {
            var rssFormatter = new Rss20FeedFormatter(feed, false);
            rssFormatter.WriteTo(writer);
            writer.Flush();
        }
        
        return Results.Text(Encoding.UTF8.GetString(stream.ToArray()), "application/rss+xml");
    }
}
