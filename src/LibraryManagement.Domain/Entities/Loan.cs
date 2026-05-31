using LibraryManagement.Domain.Common;
using LibraryManagement.Domain.Constants;
using LibraryManagement.Domain.Exceptions;

namespace LibraryManagement.Domain.Entities;

/// <summary>
/// A library loan: a <see cref="Member"/> takes a <see cref="BookCopy"/>
/// from the library for a bounded period, with an expected return date.
/// </summary>
/// <remarks>
/// State machine:
/// <list type="bullet">
///   <item>Active → Returned (normal flow: copy comes back)</item>
///   <item>Active → Overdue (DueDate passed without return)</item>
///   <item>Active → Lost (rare, copy declared lost while in possession)</item>
///   <item>Overdue → Returned (member finally returns it)</item>
///   <item>Overdue → Lost (giving up on recovery)</item>
/// </list>
/// Returned and Lost are terminal states.
/// </remarks>
public class Loan : BaseEntity<long>
{
    private const int MinLoanDurationDays = 1;
    private const int MaxLoanDurationDays = 90;

    public long BookCopyId { get; private set; }
    public long MemberId { get; private set; }
    public byte StatusId { get; private set; }
    public DateOnly LoanDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public DateOnly? ReturnDate { get; private set; }

    /// <summary>
    /// True if the loan is past its due date and has not been returned yet.
    /// Computed at read time; does not require the loan to be in the
    /// persisted Overdue state.
    /// </summary>
    public bool IsCurrentlyOverdue
    {
        get
        {
            if (ReturnDate is not null) return false;
            if (StatusId is CopyStatusIds.Lost) return false;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return DueDate < today;
        }
    }

    private Loan()
    {
    }

    /// <summary>
    /// Factory for creating a new active loan. The due date is derived from
    /// <paramref name="loanDurationDays"/>, which the application layer
    /// supplies from configuration.
    /// </summary>
    public static Loan Create(long memberId, long bookCopyId, int loanDurationDays)
    {
        ValidateMemberId(memberId);
        ValidateBookCopyId(bookCopyId);
        ValidateLoanDuration(loanDurationDays);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return new Loan
        {
            MemberId = memberId,
            BookCopyId = bookCopyId,
            LoanDate = today,
            DueDate = today.AddDays(loanDurationDays),
            ReturnDate = null,
            StatusId = LoanStatusIds.Active,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public static Loan Reconstitute(
        long id,
        long bookCopyId,
        long memberId,
        byte statusId,
        DateOnly loanDate,
        DateOnly dueDate,
        DateOnly? returnDate,
        DateTime createdAt,
        bool isDeleted)
    {
        return new Loan
        {
            Id = id,
            BookCopyId = bookCopyId,
            MemberId = memberId,
            StatusId = statusId,
            LoanDate = loanDate,
            DueDate = dueDate,
            ReturnDate = returnDate,
            CreatedAt = createdAt,
            IsDeleted = isDeleted
        };
    }

    // -----------------------------------------------------------------------
    // State transitions
    // -----------------------------------------------------------------------

    /// <summary>
    /// Closes the loan with a returned copy. Allowed from Active or Overdue.
    /// </summary>
    public void Return()
    {
        if (StatusId is not LoanStatusIds.Active and not LoanStatusIds.Overdue)
            throw new DomainException(
                "Only active or overdue loans can be returned.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (today < LoanDate)
            throw new DomainException("Return date cannot be before the loan date.");

        ReturnDate = today;
        StatusId = LoanStatusIds.Returned;
    }

    /// <summary>
    /// Moves an active loan to the persisted Overdue state.
    /// Typically called by a scheduled job once <see cref="DueDate"/> passes.
    /// </summary>
    public void MarkAsOverdue()
    {
        if (StatusId is not LoanStatusIds.Active)
            throw new DomainException("Only active loans can be marked as overdue.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (DueDate >= today)
            throw new DomainException(
                "A loan cannot be marked as overdue before its due date.");

        StatusId = LoanStatusIds.Overdue;
    }

    /// <summary>
    /// Declares the loan closed because the copy is considered unrecoverable.
    /// Allowed from Active or Overdue.
    /// </summary>
    public void MarkAsLost()
    {
        if (StatusId is not LoanStatusIds.Active and not LoanStatusIds.Overdue)
            throw new DomainException(
                "Only active or overdue loans can be marked as lost.");

        StatusId = LoanStatusIds.Lost;
        // ReturnDate stays null: the copy was never returned.
    }

    // -----------------------------------------------------------------------
    // Validation
    // -----------------------------------------------------------------------

    private static void ValidateMemberId(long memberId)
    {
        if (memberId <= 0)
            throw new DomainException("MemberId must be a positive integer.");
    }

    private static void ValidateBookCopyId(long bookCopyId)
    {
        if (bookCopyId <= 0)
            throw new DomainException("BookCopyId must be a positive integer.");
    }

    private static void ValidateLoanDuration(int days)
    {
        if (days < MinLoanDurationDays || days > MaxLoanDurationDays)
            throw new DomainException(
                $"Loan duration must be between {MinLoanDurationDays} and {MaxLoanDurationDays} days.");
    }
}