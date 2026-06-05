using LibraryManagement.Domain.Common;
using LibraryManagement.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Persistence.Repositories.Ef;

/// <summary>
/// Base implementation of <see cref="IRepository{TEntity, TId}"/> backed by
/// Entity Framework Core. Handles the CRUD primitives shared by every
/// concrete repository.
/// </summary>
/// <typeparam name="TDomain">The domain entity exposed by the repository.</typeparam>
/// <typeparam name="TDataModel">
/// The persistence model that EF Core actually tracks. Concrete repos
/// translate between the two using the mapper delegates supplied to the
/// constructor.
/// </typeparam>
/// <typeparam name="TId">The identifier type (int, long, Guid, ...).</typeparam>
public abstract class EfRepositoryBase<TDomain, TDataModel, TId> : IRepository<TDomain, TId>
    where TDomain : BaseEntity<TId>
    where TDataModel : class, new()
    where TId : notnull
{
    protected readonly LibraryDbContext Db;
    protected readonly DbSet<TDataModel> Set;
    private readonly Func<TDataModel, TDomain> _toDomain;
    private readonly Func<TDomain, TDataModel> _toDataModel;

    protected EfRepositoryBase(
        LibraryDbContext db,
        Func<TDataModel, TDomain> toDomain,
        Func<TDomain, TDataModel> toDataModel)
    {
        Db = db ?? throw new ArgumentNullException(nameof(db));
        Set = db.Set<TDataModel>();
        _toDomain = toDomain;
        _toDataModel = toDataModel;
    }

    public virtual async Task<TDomain?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        var dataModel = await Set.FindAsync(new object[] { id }, cancellationToken);
        return dataModel is null ? null : _toDomain(dataModel);
    }

    public virtual async Task<IReadOnlyList<TDomain>> ListAsync(CancellationToken cancellationToken = default)
    {
        var dataModels = await Set.AsNoTracking().ToListAsync(cancellationToken);
        return dataModels.Select(_toDomain).ToList();
    }

    public virtual async Task AddAsync(TDomain entity, CancellationToken cancellationToken = default)
    {
        var dataModel = _toDataModel(entity);
        await Set.AddAsync(dataModel, cancellationToken);
    }

    public virtual void Update(TDomain entity)
    {
        var dataModel = _toDataModel(entity);
        Set.Update(dataModel);
    }

    public virtual void Remove(TDomain entity)
    {
        entity.MarkAsDeleted();
        Update(entity);
    }
}