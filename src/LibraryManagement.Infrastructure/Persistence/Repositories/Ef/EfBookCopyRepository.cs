using LibraryManagement.Domain.Constants;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;
using LibraryManagement.Infrastructure.Persistence.DataModels;
using LibraryManagement.Infrastructure.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Persistence.Repositories.Ef;

public class EfBookCopyRepository
    : EfRepositoryBase<BookCopy, BookCopyDataModel, long>, IBookCopyRepository
{
    public EfBookCopyRepository(LibraryDbContext db)
        : base(db, BookCopyMapper.ToDomain, BookCopyMapper.ToDataModel)
    {
    }

    public async Task<BookCopy?> FindByInventoryCodeAsync(string inventoryCode, CancellationToken cancellationToken = default)
    {
        var dataModel = await Set.AsNoTracking()
            .FirstOrDefaultAsync(c => c.InventoryCode == inventoryCode, cancellationToken);
        return dataModel is null ? null : BookCopyMapper.ToDomain(dataModel);
    }

    public async Task<IReadOnlyList<BookCopy>> ListAvailableByBookIdAsync(long bookId, CancellationToken cancellationToken = default)
    {
        var dataModels = await Set.AsNoTracking()
            .Where(c => c.BookId == bookId && c.StatusId == CopyStatusIds.Available)
            .ToListAsync(cancellationToken);
        return dataModels.Select(BookCopyMapper.ToDomain).ToList();
    }
}