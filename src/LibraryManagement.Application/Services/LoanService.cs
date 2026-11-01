using FluentValidation;
using LibraryManagement.Application.Dtos.Loans;
using LibraryManagement.Application.Loans;
using LibraryManagement.Application.Loans.Interfaces;
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

    public LoanService(
        ILoanRepository loanRepo,
        IMemberRepository memberRepo,
        IBookCopyRepository copyRepo,
        IBookRepository bookRepo,
        ILoanQueries loanQueries,
        IUnitOfWork uow,
        IValidator<CreateLoanDto> createValidator,
        IValidator<LoanQueryDto> queryValidator,
        IOptions<LoanOptions> options)
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
    }

    /// <summary>
    /// Lends a copy to a member: creates an Active loan and marks the copy as
    /// Borrowed in one transaction.
    /// </summary>
    public async Task<LoanDto> CheckoutAsync(CreateLoanDto dto, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }

    /// <summary>
    /// Closes a loan as Returned and marks its copy as Available in one
    /// transaction.
    /// </summary>
    public async Task<LoanDto> ReturnAsync(long id, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
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
        await Task.CompletedTask;
        throw new NotImplementedException();
    }

    /// <summary>
    /// Lists loans for staff, filtered by member, copy and status.
    /// </summary>
    public async Task<IReadOnlyList<LoanDto>> ListAsync(LoanQueryDto query, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
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
        await Task.CompletedTask;
        throw new NotImplementedException();
    }
}
