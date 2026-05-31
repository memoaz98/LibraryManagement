using LibraryManagement.Domain.Entities;

namespace LibraryManagement.Domain.Interfaces;

public interface ILoanRepository : IRepository<Loan, long>
{
    Task<IReadOnlyList<Loan>> ListActiveByMemberAsync(long memberId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Loan>> ListOverdueAsync(CancellationToken cancellationToken = default);

    Task<int> CountActiveByMemberAsync(long memberId, CancellationToken cancellationToken = default);
}