using LibraryManagement.Domain.Interfaces;

namespace LibraryManagement.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IUnitOfWork"/>. Delegates entirely
/// to <see cref="LibraryDbContext.SaveChangesAsync(CancellationToken)"/>,
/// which already implements Unit of Work semantics internally.
/// </summary>
/// <remarks>
/// All staged changes (entities added via repositories, properties mutated
/// on tracked entities) are persisted as a single transaction by EF Core.
/// If any operation fails, the transaction is rolled back automatically.
/// </remarks>
public class EfUnitOfWork : IUnitOfWork
{
    private readonly LibraryDbContext _db;

    public EfUnitOfWork(LibraryDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}