using LibraryManagement.Application.Dtos.Books;
using LibraryManagement.Domain.Entities;

namespace LibraryManagement.Application.Mappers;

internal static class BookDtoMapper
{
    public static BookDto ToDto(Book domain)
    {
        return new BookDto(
            Id: domain.Id,
            CategoryId: domain.CategoryId,
            Title: domain.Title,
            Isbn: domain.Isbn,
            PublicationYear: domain.PublicationYear,
            Synopsis: domain.Synopsis,
            CreatedAt: domain.CreatedAt);
    }

    public static Book ToDomainForCreate(CreateBookDto dto)
    {
        return Book.Create(
            title: dto.Title,
            isbn: dto.Isbn,
            publicationYear: dto.PublicationYear,
            categoryId: dto.CategoryId,
            synopsis: dto.Synopsis);
    }
}