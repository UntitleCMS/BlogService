using Application.Common.Interfaces;
using Domain.Entites;

namespace Application.Common.Repositories;

public class ArticleFilter
{
    public int Take { get; set; } = 10;
    public string? Before { get; set; }
    public string? After { get; set; }
    public string? Of { get; set; }
    public string[]? Tags { get; set; }
    public string? SerchText { get; set; }
    public bool IsBookmarked { get; set; } = false;
    public bool IsPublish { get; set; } = true;
    public bool OnlyFollowing { get; set; } = false;

    public ArticleFilter(int take = 10, string? before = null, string? after = null, string? of = null, string[]? tags = null, bool isBookmarked = false, bool isPublish = true, bool onlyFollowing=false)
    {
        Take = take;
        Before = before;
        After = after;
        Of = of;
        Tags = tags;
        IsBookmarked = isBookmarked;
        IsPublish = isPublish;
        OnlyFollowing = onlyFollowing;
    }
}


public interface IArticlesPageableRepository
{
    Task<IResponsePageable<PostEntity>> Find(ArticleFilter fillter, string? Sub=default);
}
