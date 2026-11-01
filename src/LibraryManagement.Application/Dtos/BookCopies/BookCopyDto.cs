namespace LibraryManagement.Application.Dtos.BookCopies;

public record BookCopyDto(
    long Id,
    long BookId,
    string InventoryCode,
    string Status,
    DateOnly AcquisitionDate);
