namespace LibraryManagement.Application.Common.Exceptions;

/// <summary>
/// Reason codes a <see cref="ConflictException"/> can carry, sent in the
/// <c>code</c> field of 409 responses. They are part of the API contract:
/// clients map them to messages.
/// </summary>
public static class ConflictCodes
{
    // Loan checkout
    public const string MemberInactive = "member_inactive";
    public const string MemberHasOverdueLoan = "member_has_overdue_loan";
    public const string LoanLimitReached = "loan_limit_reached";
    public const string BookDeleted = "book_deleted";
    public const string CopyNotAvailable = "copy_not_available";
    public const string SameBookOnLoan = "same_book_on_loan";

    // Loan return
    public const string LoanNotOpen = "loan_not_open";
}
