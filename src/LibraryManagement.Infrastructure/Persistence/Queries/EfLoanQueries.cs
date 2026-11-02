using LibraryManagement.Application.Dtos.Loans;
using LibraryManagement.Application.Loans;
using LibraryManagement.Application.Loans.Interfaces;

namespace LibraryManagement.Infrastructure.Persistence.Queries;

/// <summary>
/// EF Core implementation of <see cref="ILoanQueries"/>: a read-only
/// projection that joins each loan with its member, copy and book, ignoring
/// the soft-delete filter so the history of deleted rows still displays.
/// <c>IsOverdue</c> is computed against today's date from the injected
/// <see cref="TimeProvider"/>.
/// </summary>
public class EfLoanQueries : ILoanQueries
{
    private readonly LibraryDbContext _db;
    private readonly TimeProvider _timeProvider;

    public EfLoanQueries(LibraryDbContext db, TimeProvider timeProvider)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _timeProvider = timeProvider;
    }

    public async Task<LoanDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }

    public async Task<IReadOnlyList<LoanDto>> ListAsync(
        long? memberId,
        long? bookCopyId,
        LoanStatusFilter status,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }
}
