using LibraryManagement.Application.Dtos.Loans;
using LibraryManagement.Application.Loans;
using LibraryManagement.Application.Loans.Interfaces;
using LibraryManagement.Domain.Constants;
using LibraryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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
        var row = await Rows().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        return row is null ? null : ToDto(row, GetToday());
    }

    public async Task<IReadOnlyList<LoanDto>> ListAsync(
        long? memberId,
        long? bookCopyId,
        LoanStatusFilter status,
        CancellationToken cancellationToken = default)
    {
        var rows = Rows();

        if (memberId is not null)
        {
            rows = rows.Where(r => r.MemberId == memberId);
        }

        if (bookCopyId is not null)
        {
            rows = rows.Where(r => r.BookCopyId == bookCopyId);
        }

        rows = status switch
        {
            LoanStatusFilter.Active => rows.Where(r => r.StatusId == LoanStatusIds.Active
                                                    || r.StatusId == LoanStatusIds.Overdue),
            LoanStatusFilter.Returned => rows.Where(r => r.StatusId == LoanStatusIds.Returned),
            _ => rows
        };

        var results = await rows
            .OrderByDescending(r => r.LoanDate)
            .ThenByDescending(r => r.Id)
            .ToListAsync(cancellationToken);

        var today = GetToday();
        return results.Select(row => ToDto(row, today)).ToList();
    }

    /// <summary>
    /// Loans joined with their display fields. <c>IgnoreQueryFilters</c>
    /// lifts the soft-delete filter for the whole query, so deleted loans are
    /// excluded explicitly (loans are never deleted today, but stay hidden if
    /// they ever are).
    /// </summary>
    private IQueryable<LoanRow> Rows()
    {
        return from loan in _db.Loans.IgnoreQueryFilters()
               where !loan.IsDeleted
               join member in _db.Members on loan.MemberId equals member.Id
               join copy in _db.BookCopies on loan.BookCopyId equals copy.Id
               join book in _db.Books on copy.BookId equals book.Id
               join loanStatus in _db.LoanStatuses on loan.StatusId equals loanStatus.Id
               select new LoanRow
               {
                   Id = loan.Id,
                   MemberId = loan.MemberId,
                   MemberFirstName = member.FirstName,
                   MemberLastName = member.LastName,
                   BookCopyId = loan.BookCopyId,
                   InventoryCode = copy.InventoryCode,
                   BookId = copy.BookId,
                   BookTitle = book.Title,
                   StatusId = loan.StatusId,
                   StatusName = loanStatus.Name,
                   LoanDate = loan.LoanDate,
                   DueDate = loan.DueDate,
                   ReturnDate = loan.ReturnDate,
                   CreatedAt = loan.CreatedAt
               };
    }

    private static LoanDto ToDto(LoanRow row, DateOnly today)
    {
        // "Overdue" is a domain rule: ask the entity instead of repeating it here.
        var loan = Loan.Reconstitute(
            row.Id,
            row.BookCopyId,
            row.MemberId,
            row.StatusId,
            row.LoanDate,
            row.DueDate,
            row.ReturnDate,
            row.CreatedAt,
            isDeleted: false);

        return new LoanDto(
            Id: row.Id,
            MemberId: row.MemberId,
            MemberName: $"{row.MemberFirstName} {row.MemberLastName}",
            BookCopyId: row.BookCopyId,
            InventoryCode: row.InventoryCode,
            BookId: row.BookId,
            BookTitle: row.BookTitle,
            Status: row.StatusName,
            LoanDate: row.LoanDate,
            DueDate: row.DueDate,
            ReturnDate: row.ReturnDate,
            IsOverdue: loan.IsOverdueOn(today));
    }

    private DateOnly GetToday()
    {
        return DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
    }

    // Member initializers (not a constructor) so EF can still filter and
    // sort on these properties after the projection.
    private sealed class LoanRow
    {
        public long Id { get; init; }
        public long MemberId { get; init; }
        public string MemberFirstName { get; init; } = default!;
        public string MemberLastName { get; init; } = default!;
        public long BookCopyId { get; init; }
        public string InventoryCode { get; init; } = default!;
        public long BookId { get; init; }
        public string BookTitle { get; init; } = default!;
        public byte StatusId { get; init; }
        public string StatusName { get; init; } = default!;
        public DateOnly LoanDate { get; init; }
        public DateOnly DueDate { get; init; }
        public DateOnly? ReturnDate { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
