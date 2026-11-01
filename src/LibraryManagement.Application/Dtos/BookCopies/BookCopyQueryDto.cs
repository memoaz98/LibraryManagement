namespace LibraryManagement.Application.Dtos.BookCopies;

/// <summary>
/// Filters for listing a book's copies, as received from the query string.
/// <see cref="Status"/> accepts <c>available</c> or nothing.
/// </summary>
public record BookCopyQueryDto(
    string? Status);
