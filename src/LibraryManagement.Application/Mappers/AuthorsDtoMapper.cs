using LibraryManagement.Application.Dtos.Authors;
using LibraryManagement.Domain.Entities;

namespace LibraryManagement.Application.Mappers;

internal static class AuthorsDtoMapper
{
    public static AuthorDto AuthorToDto(Author author)
    {
        return new AuthorDto(
            author.Id,
            author.FirstName,
            author.LastName,
            author.DateOfBirth);
    }

    public static Author ToDomainAuthorCreate(CreateAuthorDto dto)
    {
        return Author.Create(
            firstName: dto.FirstName,
            lastName: dto.LastName,
            dateOfBirth: dto.DateOfBirth,
            biography: dto.Biography
            );
    }
}