using LibraryManagement.Domain.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace LibraryManagement.Infrastructure.Persistence;

/// <summary>
/// ADO.NET implementation of <see cref="IUnitOfWork"/>. Owns a single
/// <see cref="SqlConnection"/> and <see cref="SqlTransaction"/> shared
/// across all repositories that participate in the same logical operation.
/// </summary>
/// <remarks>
/// Repositories obtain the connection/transaction via this object and use
/// them when executing commands. The transaction is started lazily on the
/// first repository operation and committed by <see cref="SaveChangesAsync"/>.
/// On any failure the transaction is rolled back automatically through the
/// <c>using</c>-based disposal.
/// </remarks>
public sealed class AdoUnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly string _connectionString;
    private SqlConnection? _connection;
    private SqlTransaction? _transaction;
    private int _stagedOperations;

    public AdoUnitOfWork(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("LibraryDb")
            ?? throw new InvalidOperationException(
                "Connection string 'LibraryDb' is missing from configuration.");
    }

    /// <summary>
    /// Returns the shared connection, opening it on first use.
    /// </summary>
    internal async Task<SqlConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is null)
        {
            _connection = new SqlConnection(_connectionString);
            await _connection.OpenAsync(cancellationToken);
        }
        return _connection;
    }

    /// <summary>
    /// Returns the shared transaction, beginning one on first use.
    /// All commands executed within the same UoW must use this transaction.
    /// </summary>
    internal async Task<SqlTransaction> GetTransactionAsync(CancellationToken cancellationToken)
    {
        if (_transaction is null)
        {
            var connection = await GetConnectionAsync(cancellationToken);
            _transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        }
        return _transaction;
    }

    /// <summary>
    /// Bumps the count of staged operations. Repositories call this on each
    /// add/update/remove so SaveChangesAsync can return a meaningful number.
    /// </summary>
    internal void RegisterStagedOperation()
    {
        _stagedOperations++;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return 0;
        }

        try
        {
            await _transaction.CommitAsync(cancellationToken);
            var committed = _stagedOperations;
            _stagedOperations = 0;
            return committed;
        }
        catch
        {
            await _transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}