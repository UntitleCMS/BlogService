using Application.Common.Interfaces;
using Domain.Entites;

namespace Application.Common.Repositories;

public interface IPostRepository
{
    Task Add(ref PostEntity post, CancellationToken cancellationToken = default);
    Task Update(PostEntity post, CancellationToken cancellationToken = default);
    Task<PostEntity> FindById(string id, string? sub = default, CancellationToken cancellationToken = default);
    Task Delete(string id, string? sub = default, CancellationToken cancellationToken = default);
    Task Like(string id, string sub, CancellationToken cancellationToken = default);
    Task UnLike(string id, string sub, CancellationToken cancellationToken = default);
    Task Save(string id, string sub, CancellationToken cancellationToken = default);
    Task UnSave(string id, string sub, CancellationToken cancellationToken = default);
    Task Publish(string id, string sub, CancellationToken cancellationToken = default);
    Task Unpublish(string id, string sub, CancellationToken cancellationToken = default);
    Task<IResponsePageable<PostEntity>> Find(int Take, string? Befor=default, string? After=default, string? Of=default, string[]? Tags=default, string? Sub=default);
}
