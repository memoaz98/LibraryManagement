using LibraryManagement.Domain.Entities;

namespace LibraryManagement.Domain.Interfaces;

public interface IAuthorRepository : IRepository<Author, long>
{
    Task<IReadOnlyList<Author>> FindByLastNameAsync(string lastNamePartial, CancellationToken cancellationToken = default);
}