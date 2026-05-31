using LibraryManagement.Domain.Common;

namespace LibraryManagement.Domain.Interfaces;

/// <summary>
/// Generic repository contract for domain entities that inherit from
/// <see cref="BaseEntity{TId}"/>. Defines the CRUD primitives shared by
/// every entity. Specialized queries live in per-entity sub-interfaces.
/// </summary>
/// <remarks>
/// Implementations do NOT commit changes — that is the responsibility of
/// <see cref="IUnitOfWork.SaveChangesAsync(CancellationToken)"/>. The
/// repository merely stages mutations against the underlying store.
/// </remarks>
public interface IRepository<TEntity, in TId>
    where TEntity : BaseEntity<TId>
    where TId : notnull
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Remove(TEntity entity);
}