using LibraryManagement.Application.Dtos.Loans;

namespace LibraryManagement.Application.Loans.Interfaces;

/// <summary>
/// Read-only queries that return loans ready for display: each
/// <see cref="LoanDto"/> carries the member name, inventory code and book title.
/// </summary>
/// <remarks>
/// Display fields load even when the member, copy or book is soft-deleted, so
/// loan history still displays. Lists are sorted by loan date (newest first),
/// then by id. Writes go through <c>ILoanRepository</c>, not through here.
/// </remarks>
public interface ILoanQueries
{
    Task<LoanDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LoanDto>> ListAsync(
        long? memberId,
        long? bookCopyId,
        LoanStatusFilter status,
        CancellationToken cancellationToken = default);
}
