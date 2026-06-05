using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;
using LibraryManagement.Infrastructure.Persistence.DataModels;
using LibraryManagement.Infrastructure.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Persistence.Repositories.Ef;

public class EfBookRepository
    : EfRepositoryBase<Book, BookDataModel, long>, IBookRepository
{
    public EfBookRepository(LibraryDbContext db)
        : base(db, BookMapper.ToDomain, BookMapper.ToDataModel)
    {
    }

    public async Task<Book?> FindByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
    {
        var dataModel = await Set.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Isbn == isbn, cancellationToken);
        return dataModel is null ? null : BookMapper.ToDomain(dataModel);
    }

    public Task<bool> IsbnExistsAsync(string isbn, CancellationToken cancellationToken = default)
    {
        return Set.AsNoTracking().AnyAsync(b => b.Isbn == isbn, cancellationToken);
    }

    public async Task<IReadOnlyList<Book>> FindByCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var dataModels = await Set.AsNoTracking()
            .Where(b => b.CategoryId == categoryId)
            .ToListAsync(cancellationToken);
        return dataModels.Select(BookMapper.ToDomain).ToList();
    }
}