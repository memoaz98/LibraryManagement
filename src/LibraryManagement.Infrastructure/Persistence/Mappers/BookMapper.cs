using LibraryManagement.Domain.Entities;
using LibraryManagement.Infrastructure.Persistence.DataModels;

namespace LibraryManagement.Infrastructure.Persistence.Mappers;

internal static class BookMapper
{
    public static Book ToDomain(BookDataModel dataModel)
    {
        return Book.Reconstitute(
            id: dataModel.Id,
            title: dataModel.Title,
            isbn: dataModel.Isbn,
            publicationYear: dataModel.PublicationYear,
            categoryId: dataModel.CategoryId,
            synopsis: dataModel.Synopsis,
            createdAt: dataModel.CreatedAt,
            isDeleted: dataModel.IsDeleted);
    }

    public static BookDataModel ToDataModel(Book domain)
    {
        return new BookDataModel
        {
            Id = domain.Id,
            Title = domain.Title,
            Isbn = domain.Isbn,
            PublicationYear = domain.PublicationYear,
            CategoryId = domain.CategoryId,
            Synopsis = domain.Synopsis,
            CreatedAt = domain.CreatedAt,
            IsDeleted = domain.IsDeleted
        };
    }
}