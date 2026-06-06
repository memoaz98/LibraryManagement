namespace LibraryManagement.Application.Dtos.Books;

/// <summary>
/// Update DTO. Note that ISBN and PublicationYear are intentionally absent —
/// they are immutable in the domain (the book's identifier and historical
/// publication date should not change after creation).
/// </summary>
public record UpdateBookDto(
    string Title,
    int CategoryId,
    string? Synopsis);