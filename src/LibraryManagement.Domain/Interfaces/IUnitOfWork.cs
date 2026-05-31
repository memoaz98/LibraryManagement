namespace LibraryManagement.Domain.Interfaces;

/// <summary>
/// Coordinates the atomic persistence of changes made through one or more
/// repositories during a single business operation.
/// </summary>
/// <remarks>
/// Repositories stage mutations (add/update/remove) in memory. The actual
/// transaction is committed by <see cref="SaveChangesAsync"/>. If any part
/// of the operation fails, the entire unit is rolled back.
/// <para>
/// Implemented by EF Core's <c>DbContext</c> implicitly; the ADO.NET
/// implementation wraps a single <c>SqlTransaction</c> shared across repos.
/// </para>
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>
    /// Commits all pending changes as a single transaction.
    /// </summary>
    /// <returns>The number of state entries written to the underlying store.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}