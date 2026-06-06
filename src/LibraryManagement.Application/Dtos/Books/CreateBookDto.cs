namespace LibraryManagement.Application.Dtos.Books;

public record CreateBookDto(
    string Title,
    string Isbn,
    short PublicationYear,
    int CategoryId,
    string? Synopsis);