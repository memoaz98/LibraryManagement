using LibraryManagement.Domain.Entities;
using LibraryManagement.Infrastructure.Persistence.DataModels;

namespace LibraryManagement.Infrastructure.Persistence.Mappers;

internal static class AuthorMapper
{
    public static Author ToDomain(AuthorDataModel dataModel)
    {
        return Author.Reconstitute(
            id: dataModel.Id,
            firstName: dataModel.FirstName,
            lastName: dataModel.LastName,
            dateOfBirth: dataModel.DateOfBirth,
            biography: dataModel.Biography,
            createdAt: dataModel.CreatedAt,
            isDeleted: dataModel.IsDeleted);
    }

    public static AuthorDataModel ToDataModel(Author domain)
    {
        return new AuthorDataModel
        {
            Id = domain.Id,
            FirstName = domain.FirstName,
            LastName = domain.LastName,
            DateOfBirth = domain.DateOfBirth,
            Biography = domain.Biography,
            CreatedAt = domain.CreatedAt,
            IsDeleted = domain.IsDeleted
        };
    }
}