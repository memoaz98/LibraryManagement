namespace LibraryManagement.Application.Dtos.Loans;

/// <summary>
/// Read-side DTO. Carries display fields so the client needs no extra calls.
/// <see cref="IsOverdue"/> is computed: no loan is ever stored as Overdue.
/// </summary>
public record LoanDto(
    long Id,
    long MemberId,
    string MemberName,
    long BookCopyId,
    string InventoryCode,
    long BookId,
    string BookTitle,
    string Status,
    DateOnly LoanDate,
    DateOnly DueDate,
    DateOnly? ReturnDate,
    bool IsOverdue);
