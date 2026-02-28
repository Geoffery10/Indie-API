using IndieAPI.Api.Models;
using Microsoft.AspNetCore.Http;

namespace IndieAPI.Api.Interfaces;

public interface IProjectService
{
    Task<PagedProjectResult> GetPagedProjectsAsync(int page, int pageSize);
    Task<FullArticle?> GetArticleAsync(string linkId);
    IResult GetAsset(string path); 
}