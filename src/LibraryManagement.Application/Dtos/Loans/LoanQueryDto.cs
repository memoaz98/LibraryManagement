namespace LibraryManagement.Application.Dtos.Loans;

/// <summary>
/// Filters for listing loans, as received from the query string.
/// <see cref="Status"/> accepts <c>active</c>, <c>returned</c> or nothing.
/// </summary>
public record LoanQueryDto(
    long? MemberId,
    long? BookCopyId,
    string? Status);
