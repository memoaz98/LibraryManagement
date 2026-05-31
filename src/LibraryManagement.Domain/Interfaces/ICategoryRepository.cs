using LibraryManagement.Domain.Entities;

namespace LibraryManagement.Domain.Interfaces;

public interface ICategoryRepository : IRepository<Category, int>
{
    Task<Category?> FindByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);
}