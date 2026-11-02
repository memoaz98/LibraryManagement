using LibraryManagement.Application.Dtos.BookCopies;
using LibraryManagement.Domain.Constants;
using LibraryManagement.Domain.Entities;

namespace LibraryManagement.Application.Mappers;

internal static class BookCopyDtoMapper
{
    public static BookCopyDto ToDto(BookCopy domain)
    {
        return new BookCopyDto(
            Id: domain.Id,
            BookId: domain.BookId,
            InventoryCode: domain.InventoryCode,
            Status: ToStatusName(domain.StatusId),
            AcquisitionDate: domain.AcquisitionDate);
    }

    // Same names as the CopyStatus catalog rows seeded by V002.
    private static string ToStatusName(byte statusId)
    {
        return statusId switch
        {
            CopyStatusIds.Available => "Available",
            CopyStatusIds.Borrowed => "Borrowed",
            CopyStatusIds.Maintenance => "Maintenance",
            CopyStatusIds.Lost => "Lost",
            _ => throw new ArgumentOutOfRangeException(nameof(statusId), statusId, "Unknown copy status.")
        };
    }
}
