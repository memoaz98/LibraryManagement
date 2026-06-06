namespace LibraryManagement.Application.Dtos.Books;

public record BookDto(
    long Id,
    int CategoryId,
    string Title,
    string Isbn,
    short PublicationYear,
    string? Synopsis,
    DateTime CreatedAt);