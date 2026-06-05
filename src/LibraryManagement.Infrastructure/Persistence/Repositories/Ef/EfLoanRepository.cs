using LibraryManagement.Domain.Constants;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;
using LibraryManagement.Infrastructure.Persistence.DataModels;
using LibraryManagement.Infrastructure.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Persistence.Repositories.Ef;

public class EfLoanRepository
    : EfRepositoryBase<Loan, LoanDataModel, long>, ILoanRepository
{
    public EfLoanRepository(LibraryDbContext db)
        : base(db, LoanMapper.ToDomain, LoanMapper.ToDataModel)
    {
    }

    public async Task<IReadOnlyList<Loan>> ListActiveByMemberAsync(long memberId, CancellationToken cancellationToken = default)
    {
        var dataModels = await Set.AsNoTracking()
            .Where(l => l.MemberId == memberId
                     && (l.StatusId == LoanStatusIds.Active || l.StatusId == LoanStatusIds.Overdue))
            .ToListAsync(cancellationToken);
        return dataModels.Select(LoanMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Loan>> ListOverdueAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dataModels = await Set.AsNoTracking()
            .Where(l => l.ReturnDate == null
                     && l.DueDate < today
                     && l.StatusId != LoanStatusIds.Lost)
            .ToListAsync(cancellationToken);
        return dataModels.Select(LoanMapper.ToDomain).ToList();
    }

    public Task<int> CountActiveByMemberAsync(long memberId, CancellationToken cancellationToken = default)
    {
        return Set.AsNoTracking()
            .CountAsync(l => l.MemberId == memberId
                          && (l.StatusId == LoanStatusIds.Active || l.StatusId == LoanStatusIds.Overdue),
                cancellationToken);
    }
}