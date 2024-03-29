using Application.Common.Interfaces;
using Application.Common.models;
using Application.Common.Repositories;
using Domain.Entites;
using Domain.Exceptions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Collections;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Infrastructure.Repositories;

public class PostRepository : IPostRepository
{
    private readonly ILogger<PostRepository> _logger;
    private readonly IMongoCollection<PostCollection> _collection;

    public PostRepository(DataContext mongo, ILogger<PostRepository> logger)
    {
        _collection = mongo.Collection<PostCollection>();
        _logger = logger;
    }

    public Task Delete(string id, string? sub = null, CancellationToken cancellationToken = default)
    {
        var Id = new Guid(Base64UrlEncoder.DecodeBytes(id));
        var filter = Builders<PostCollection>.Filter.And(
            Builders<PostCollection>.Filter.Eq(i => i.ID, Id),
            Builders<PostCollection>.Filter.Eq(i => i.AuthorId, sub));

        var a = _collection.DeleteOneAsync(filter, cancellationToken);

        if (a.IsCompletedSuccessfully && a.Exception is not null)
            return Task.FromException(a.Exception);

        if (a.Result.DeletedCount < 1)
            return Task.FromException(new ArticleNotFoundException());

        if (a.Result.DeletedCount > 1)
            return Task.FromException(new Exception("Delete Too Many Article"));

        _logger.LogInformation("Aritcel[{}] by[{}] is deleted", id, sub);

        return Task.CompletedTask;
    }

    public Task<PostEntity> FindById(string id, string? sub = null, CancellationToken cancellationToken = default)
    {
        try
        {
            Guid Id;
            Id = new Guid(Base64UrlEncoder.DecodeBytes(id));
            var result = _collection.AsQueryable()
                .Where(i => i.ID == Id)
                .Select(article => new PostEntity()
                {
                    ID = article.ID,
                    AuthorId = article.AuthorId,

                    Title = article.Title,
                    Cover = article.Cover,
                    Content = article.Content,
                    ContentPreviews = article.ContentPreviews,
                    Tags = article.Tags,

                    IsPublished = article.IsPublished,
                    CreatedAt = article.Timestamp,
                    LastUpdated = article.LastUpdate,

                    SavedBy = article.SavedBy.Where(i => i == sub).ToArray(),
                    LikedBy = article.LikedBy.Where(i => i == sub).ToArray(),
                    LikedCount = article.LikedBy.Count()
                }).FirstOrDefault();


            if (result is null)
                return Task.FromException<PostEntity>(new ArticleNotFoundException());

            _logger.LogInformation("Find post[{postID}] with sub[{sub}] : {result}", id, sub, result.ToJson(new() { Indent = true }));

            return Task.FromResult(result);

        }
        catch (Exception ex)
        {
            return Task.FromException<PostEntity>(ex);
        }

    }

    public Task Like(string id, string sub, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<PostCollection>.Filter
                .Eq(i => i.ID, new Guid(Base64UrlEncoder.DecodeBytes(id)));

            var update = Builders<PostCollection>.Update
                .AddToSet(i => i.LikedBy, sub);

            var res = _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

            if (res.Result.MatchedCount == 0)
                return Task.FromException(new ArticleNotFoundException());

            if (res.Result.ModifiedCount == 0)
                return Task.FromException(new Exception("Not Change"));

            _logger.LogInformation("user[{}] is like article[{}]", id, sub);

            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            return Task.FromException(e);
        }
    }

    public async Task Publish(string id, string sub, CancellationToken cancellationToken = default)
    {
        var filter = Builders<PostCollection>.Filter.And(
            Builders<PostCollection>.Filter.Eq(i => i.AuthorId, sub),
            Builders<PostCollection>.Filter.Eq(i => i.ID, new Guid(Base64UrlEncoder.DecodeBytes(id))));

        var update = Builders<PostCollection>.Update
            .Set(i => i.IsPublished, true);

        var res = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

        if (res.MatchedCount == 0)
            throw new ArticleNotFoundException();

        if (res.ModifiedCount == 0)
            throw new Exception("Not Change");

        _logger.LogInformation("user[{}] is publish article[{}]", id, sub);
    }

    public Task Save(string id, string sub, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<PostCollection>.Filter
                .Eq(i => i.ID, new Guid(Base64UrlEncoder.DecodeBytes(id)));

            var update = Builders<PostCollection>.Update
                .AddToSet(i => i.SavedBy, sub);

            var res = _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

            if (res.Result.MatchedCount == 0)
                return Task.FromException(new ArticleNotFoundException());

            if (res.Result.ModifiedCount == 0)
                return Task.FromException(new Exception("Not Change"));

            _logger.LogInformation("user[{}] is save article[{}]", id, sub);

            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            return Task.FromException(e);
        }

    }

    public Task Add(ref PostEntity post, CancellationToken cancellationToken = default)
    {
        try
        {
            if (post.AuthorId.IsNullOrEmpty())
                throw new ArgumentException("Author Id Should Not Be Null or Empty.");
            var newPost = new PostCollection()
            {
                Content = post.Content,
                ContentPreviews = post.ContentPreviews,
                Cover = post.Cover,
                IsPublished = post.IsPublished,
                Tags = post.Tags,
                Title = post.Title,
                AuthorId = post.AuthorId,
                LastUpdate = DateTime.UtcNow,
                Timestamp = DateTime.UtcNow
            };

            _collection
                .InsertOneAsync(newPost, cancellationToken: cancellationToken)
                .Wait(cancellationToken);

            post.ID = newPost.ID;
            post.LastUpdated = newPost.LastUpdate;
            post.CreatedAt = newPost.Timestamp;

        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }

        return Task.CompletedTask;
    }

    public Task UnLike(string id, string sub, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<PostCollection>.Filter
                .Eq(i => i.ID, new Guid(Base64UrlEncoder.DecodeBytes(id)));

            var update = Builders<PostCollection>.Update
                .Pull(i => i.LikedBy, sub);

            var res = _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

            if (res.Result.MatchedCount == 0)
                return Task.FromException(new ArticleNotFoundException());

            if (res.Result.ModifiedCount == 0)
                return Task.FromException(new Exception("Not Change"));

            _logger.LogInformation("user[{}] is unlike article[{}]", id, sub);

            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            return Task.FromException(e);
        }
    }

    public async Task Unpublish(string id, string sub, CancellationToken cancellationToken = default)
    {
        var filter = Builders<PostCollection>.Filter.And(
            Builders<PostCollection>.Filter.Eq(i => i.AuthorId, sub),
            Builders<PostCollection>.Filter.Eq(i => i.ID, new Guid(Base64UrlEncoder.DecodeBytes(id))));

        var update = Builders<PostCollection>.Update
            .Set(i => i.IsPublished, false);

        var res = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

        if (res.MatchedCount == 0)
            throw new ArticleNotFoundException();

        if (res.ModifiedCount == 0)
            throw new Exception("Not Change");

        _logger.LogInformation("user[{}] is un-publish article[{}]", id, sub);

    }

    public Task UnSave(string id, string sub, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<PostCollection>.Filter
                .Eq(i => i.ID, new Guid(Base64UrlEncoder.DecodeBytes(id)));

            var update = Builders<PostCollection>.Update
                .Pull(i => i.SavedBy, sub);

            var res = _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

            if (res.Result.MatchedCount == 0)
                return Task.FromException(new ArticleNotFoundException());

            if (res.Result.ModifiedCount == 0)
                return Task.FromException(new Exception("Not Change"));

            _logger.LogInformation("user[{}] is un-save article[{}]", id, sub);

            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            return Task.FromException(e);
        }
    }

    public Task Update(PostEntity post, CancellationToken cancellationToken = default)
    {
        var updateSet = Builders<PostCollection>.Update
            .Set(i => i.Title, post.Title)
            .Set(i => i.Content, post.Content)
            .Set(i => i.ContentPreviews, post.ContentPreviews)
            .Set(i => i.IsPublished, post.IsPublished)
            .Set(i => i.Cover, post.Cover)
            .Set(i => i.Tags, post.Tags)
            .Set(i => i.LastUpdate, DateTime.Now);


        var FilterBuilder = Builders<PostCollection>.Filter;
        var fillterAuthor = FilterBuilder.And(
            FilterBuilder.Eq(i => i.ID, post.ID),
            FilterBuilder.Eq(i => i.AuthorId, post.AuthorId));

        var a = _collection.UpdateOneAsync(
            fillterAuthor,
            updateSet,
            cancellationToken: cancellationToken);

        if (a.IsCompleted && a.Exception is not null)
            return Task.FromException(a.Exception);

        Console.WriteLine(a.Result.MatchedCount);
        Console.WriteLine(a.Result.ModifiedCount);

        if (a.Result.MatchedCount == 0)
            return Task.FromException(new ArticleNotFoundException());

        if (a.Result.ModifiedCount == 0)
            return Task.FromException(new Exception("No Change."));

        return Task.CompletedTask;
    }

    public async Task<IResponsePageable<PostEntity>> Find
        (
            int Take,
            string? Before = default,
            string? After = default,
            string? Of = default,
            string[]? Tags = default,
            string? Sub = default
        )
    {
        if (After is not null && Before is not null)
        {
            throw new Exception("Befor and After should not have value in the same time.");
        }

        Pageable<PostEntity> data = new();
        var countAll = _collection.EstimatedDocumentCount();

        // Default
        if (Before is null && After is null)
        {
            var res = GetPostEntityQueryable(Sub)
                .Where(i=> Tags == null || i.Tags.Any(x=>Tags.Contains(x)))
                .Where(i => Of == null || i.AuthorId == Of)
                .OrderByDescending(i => i.CreatedAt)
                .Take(Take + 1)
                //.OrderBy(i=>i.CreatedAt)
                .ToList();

            data.HasNext = false;
            data.HasPrevious = res.Count > Take;
            data.Collections = res.Take(Take);
            data.CountAll = countAll;
        }

        // Before
        if (Before is not null && After is null)
        {
            var docref = _collection
                .Find(i => i.ID == new Guid(Base64UrlEncoder.DecodeBytes(Before)))
                .First();

            var res = GetPostEntityQueryable(Sub)
                .Where(i => Of == null || i.AuthorId == Of)
                .Where(i => i.CreatedAt < docref.Timestamp)
                .OrderByDescending(i => i.CreatedAt)
                .Take(Take + 1)
                .ToList();

            var hasNewer = GetPostEntityQueryable()
                .Where(i => i.CreatedAt >= docref.Timestamp)
                .Take(1)
                .Count();

            data.HasNext = hasNewer == 1;
            data.HasPrevious = res.Count > Take;
            data.Collections = res.Take(Take);
            data.Pivot = Before;
            data.CountAll = countAll;
        }

        // After 
        if (Before is null && After is not null)
        {
            var docref = _collection
                .Find(i => i.ID == new Guid(Base64UrlEncoder.DecodeBytes(After)))
                .First();
            var res = GetPostEntityQueryable(Sub)
                .Where(i => Of == null || i.AuthorId == Of)
                .Where(i => i.CreatedAt > docref.Timestamp)
                .Take(Take + 1)
                .ToList();

            var hasBefore = GetPostEntityQueryable(Sub)
                .Where(i => i.CreatedAt <= docref.Timestamp)
                .Take(1)
                .Count();

            data.HasPrevious = hasBefore == 1;
            data.HasNext = res.Count > Take;
            data.Collections = res.Take(Take);
            data.Pivot = After;
            data.CountAll = countAll;
        }

        //_logger.LogInformation(">> {}", data.ToJson(new() { Indent = true }));

        return data;
    }
    private IQueryable<PostEntity> GetPostEntityQueryable
        (string? Sub = default)
    {
        var res = _collection.AsQueryable()
            .OrderBy(i => i.Timestamp)
           .Select(article => new PostEntity()
           {
               ID = article.ID,
               AuthorId = article.AuthorId,

               Title = article.Title,
               Cover = article.Cover,
               Content = article.Content,
               ContentPreviews = article.ContentPreviews,
               Tags = article.Tags,

               IsPublished = article.IsPublished,
               CreatedAt = article.Timestamp,
               LastUpdated = article.LastUpdate,

               SavedBy = article.SavedBy.Where(i => i == Sub).ToList(),
               LikedBy = article.LikedBy.Where(i => i == Sub).ToList(),
               LikedCount = article.LikedBy.Count()
           });
        return res;
    }
}
