using LibraryManagement.Domain.Entities;
using LibraryManagement.Infrastructure.Persistence.DataModels;

namespace LibraryManagement.Infrastructure.Persistence.Mappers;

internal static class BookCopyMapper
{
    public static BookCopy ToDomain(BookCopyDataModel dataModel)
    {
        return BookCopy.Reconstitute(
            id: dataModel.Id,
            bookId: dataModel.BookId,
            statusId: dataModel.StatusId,
            inventoryCode: dataModel.InventoryCode,
            acquisitionDate: dataModel.AcquisitionDate,
            createdAt: dataModel.CreatedAt,
            isDeleted: dataModel.IsDeleted);
    }

    public static BookCopyDataModel ToDataModel(BookCopy domain)
    {
        return new BookCopyDataModel
        {
            Id = domain.Id,
            BookId = domain.BookId,
            StatusId = domain.StatusId,
            InventoryCode = domain.InventoryCode,
            AcquisitionDate = domain.AcquisitionDate,
            CreatedAt = domain.CreatedAt,
            IsDeleted = domain.IsDeleted
        };
    }
}