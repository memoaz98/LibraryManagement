using LibraryManagement.Application.Common.Exceptions;
using LibraryManagement.Domain.Interfaces;
using LibraryManagement.Infrastructure.Persistence.Configurations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

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
/// <para>
/// A unique-index violation means another request got there first, so it is
/// rethrown as a <see cref="ConflictException"/> (HTTP 409) instead of
/// surfacing as a 500.
/// </para>
/// </remarks>
public class EfUnitOfWork : IUnitOfWork
{
    // SQL Server: duplicate key in a unique index (2601) or constraint (2627).
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    private readonly LibraryDbContext _db;

    public EfUnitOfWork(LibraryDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException
        {
            Number: UniqueIndexViolation or UniqueConstraintViolation
        } sqlException)
        {
            throw ToConflict(sqlException, ex);
        }
    }

    private static ConflictException ToConflict(SqlException sqlException, DbUpdateException ex)
    {
        if (sqlException.Message.Contains(LoanConfiguration.OpenLoanPerCopyIndexName, StringComparison.Ordinal))
        {
            return new ConflictException(
                "The copy is already on loan.",
                ConflictCodes.CopyNotAvailable,
                ex);
        }

        return new ConflictException(
            "The change conflicts with data that already exists.",
            code: null,
            ex);
    }
}
