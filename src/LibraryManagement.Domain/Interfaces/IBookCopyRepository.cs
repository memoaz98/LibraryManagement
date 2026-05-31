using LibraryManagement.Domain.Entities;

namespace LibraryManagement.Domain.Interfaces;

public interface IBookCopyRepository : IRepository<BookCopy, long>
{
    Task<BookCopy?> FindByInventoryCodeAsync(string inventoryCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookCopy>> ListAvailableByBookIdAsync(long bookId, CancellationToken cancellationToken = default);
}