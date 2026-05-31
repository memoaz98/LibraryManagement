using LibraryManagement.Domain.Entities;

namespace LibraryManagement.Domain.Interfaces;

public interface IBookRepository : IRepository<Book, long>
{
    Task<Book?> FindByIsbnAsync(string isbn, CancellationToken cancellationToken = default);

    Task<bool> IsbnExistsAsync(string isbn, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Book>> FindByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
}