using FluentValidation;
using LibraryManagement.Application.Common.Exceptions;
using LibraryManagement.Application.Dtos.Loans;
using LibraryManagement.Application.Loans;
using LibraryManagement.Application.Loans.Interfaces;
using LibraryManagement.Domain.Constants;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Exceptions;
using LibraryManagement.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace LibraryManagement.Application.Services;

/// <summary>
/// Coordinates the loan use cases: checkout, return, and reading loans.
/// Writes go through the repositories and the unit of work; reads go
/// through <see cref="ILoanQueries"/>.
/// </summary>
public class LoanService
{
    private readonly ILoanRepository _loanRepo;
    private readonly IMemberRepository _memberRepo;
    private readonly IBookCopyRepository _copyRepo;
    private readonly IBookRepository _bookRepo;
    private readonly ILoanQueries _loanQueries;
    private readonly IUnitOfWork _uow;
    private readonly IValidator<CreateLoanDto> _createValidator;
    private readonly IValidator<LoanQueryDto> _queryValidator;
    private readonly LoanOptions _options;
    private readonly TimeProvider _timeProvider;

    public LoanService(
        ILoanRepository loanRepo,
        IMemberRepository memberRepo,
        IBookCopyRepository copyRepo,
        IBookRepository bookRepo,
        ILoanQueries loanQueries,
        IUnitOfWork uow,
        IValidator<CreateLoanDto> createValidator,
        IValidator<LoanQueryDto> queryValidator,
        IOptions<LoanOptions> options,
        TimeProvider timeProvider)
    {
        _loanRepo = loanRepo;
        _memberRepo = memberRepo;
        _copyRepo = copyRepo;
        _bookRepo = bookRepo;
        _loanQueries = loanQueries;
        _uow = uow;
        _createValidator = createValidator;
        _queryValidator = queryValidator;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Lends a copy to a member: creates an Active loan and marks the copy as
    /// Borrowed in one transaction.
    /// </summary>
    /// <remarks>
    /// The checks run in the order the feature spec documents, so the first
    /// broken rule decides the response.
    /// </remarks>
    public async Task<LoanDto> CheckoutAsync(CreateLoanDto dto, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(dto, cancellationToken);

        var member = await _memberRepo.GetByIdAsync(dto.MemberId, cancellationToken)
            ?? throw new NotFoundException(nameof(Member), dto.MemberId);

        var copy = await _copyRepo.GetByIdAsync(dto.BookCopyId, cancellationToken)
            ?? throw new NotFoundException(nameof(BookCopy), dto.BookCopyId);

        if (!member.IsActive)
        {
            throw new ConflictException(
                $"Member {member.Id} is inactive and cannot borrow.",
                ConflictCodes.MemberInactive);
        }

        var today = GetToday();
        var openLoans = await _loanRepo.ListActiveByMemberAsync(member.Id, cancellationToken);

        if (openLoans.Any(loan => loan.IsOverdueOn(today)))
        {
            throw new ConflictException(
                $"Member {member.Id} has an overdue loan and cannot borrow until it is returned.",
                ConflictCodes.MemberHasOverdueLoan);
        }

        if (openLoans.Count >= _options.MaxActiveLoansPerMember)
        {
            throw new ConflictException(
                $"Member {member.Id} already has {openLoans.Count} open loans; the limit is {_options.MaxActiveLoansPerMember}.",
                ConflictCodes.LoanLimitReached);
        }

        if (await _bookRepo.GetByIdAsync(copy.BookId, cancellationToken) is null)
        {
            throw new ConflictException(
                $"The book of copy {copy.Id} has been deleted, so the copy cannot be lent.",
                ConflictCodes.BookDeleted);
        }

        if (copy.StatusId != CopyStatusIds.Available)
        {
            throw new ConflictException(
                $"Copy {copy.Id} is not available.",
                ConflictCodes.CopyNotAvailable);
        }

        if (await _loanRepo.HasOpenLoanForBookAsync(member.Id, copy.BookId, cancellationToken))
        {
            throw new ConflictException(
                $"Member {member.Id} already has a copy of this book on loan.",
                ConflictCodes.SameBookOnLoan);
        }

        var loan = Loan.Create(member.Id, copy.Id, _options.DefaultLoanDays, today);
        copy.MarkAsBorrowed();

        await _loanRepo.AddAsync(loan, cancellationToken);
        _copyRepo.Update(copy);
        await _uow.SaveChangesAsync(cancellationToken);

        return await GetLoanDtoAsync(loan.Id, cancellationToken);
    }

    /// <summary>
    /// Closes a loan as Returned and marks its copy as Available in one
    /// transaction. The member's state and the book do not matter: a
    /// suspended member can always bring a copy back.
    /// </summary>
    public async Task<LoanDto> ReturnAsync(long id, CancellationToken cancellationToken)
    {
        var loan = await _loanRepo.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Loan), id);

        if (loan.StatusId is not LoanStatusIds.Active and not LoanStatusIds.Overdue)
        {
            throw new ConflictException(
                $"Loan {loan.Id} is already closed and cannot be returned.",
                ConflictCodes.LoanNotOpen);
        }

        var copy = await _copyRepo.GetByIdAsync(loan.BookCopyId, cancellationToken)
            ?? throw new NotFoundException(nameof(BookCopy), loan.BookCopyId);

        loan.Return(GetToday());
        copy.MarkAsReturned();

        _loanRepo.Update(loan);
        _copyRepo.Update(copy);
        await _uow.SaveChangesAsync(cancellationToken);

        return await GetLoanDtoAsync(loan.Id, cancellationToken);
    }

    /// <summary>
    /// Gets one loan. When <paramref name="canReadAll"/> is false (a Reader),
    /// a loan that belongs to another member is reported as not found.
    /// </summary>
    public async Task<LoanDto> GetByIdAsync(
        long id,
        string userId,
        bool canReadAll,
        CancellationToken cancellationToken)
    {
        var loan = await _loanQueries.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Loan), id);

        if (!canReadAll)
        {
            var member = await _memberRepo.FindByUserIdAsync(userId, cancellationToken);
            if (member is null || member.Id != loan.MemberId)
            {
                // Same answer as a missing loan, so its existence is not revealed.
                throw new NotFoundException(nameof(Loan), id);
            }
        }

        return loan;
    }

    /// <summary>
    /// Lists loans for staff, filtered by member, copy and status.
    /// </summary>
    public async Task<IReadOnlyList<LoanDto>> ListAsync(LoanQueryDto query, CancellationToken cancellationToken)
    {
        await _queryValidator.ValidateAndThrowAsync(query, cancellationToken);

        return await _loanQueries.ListAsync(
            query.MemberId,
            query.BookCopyId,
            LoanStatusFilterParser.Parse(query.Status),
            cancellationToken);
    }

    /// <summary>
    /// Lists the loans of the member linked to <paramref name="userId"/>.
    /// Returns an empty list when no member is linked.
    /// </summary>
    public async Task<IReadOnlyList<LoanDto>> ListForUserAsync(
        string userId,
        string? status,
        CancellationToken cancellationToken)
    {
        await _queryValidator.ValidateAndThrowAsync(new LoanQueryDto(null, null, status), cancellationToken);

        var member = await _memberRepo.FindByUserIdAsync(userId, cancellationToken);
        if (member is null)
        {
            return [];
        }

        return await _loanQueries.ListAsync(
            member.Id,
            bookCopyId: null,
            LoanStatusFilterParser.Parse(status),
            cancellationToken);
    }

    private DateOnly GetToday()
    {
        return DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
    }

    private async Task<LoanDto> GetLoanDtoAsync(long id, CancellationToken cancellationToken)
    {
        return await _loanQueries.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Loan), id);
    }
}
