namespace LibraryManagement.Application.Dtos.Loans;

/// <summary>
/// Checkout request: lend a copy to a member.
/// </summary>
public record CreateLoanDto(
    long MemberId,
    long BookCopyId);
