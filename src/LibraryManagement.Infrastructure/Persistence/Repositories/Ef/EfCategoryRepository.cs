using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;
using LibraryManagement.Infrastructure.Persistence.DataModels;
using LibraryManagement.Infrastructure.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Persistence.Repositories.Ef;

public class EfCategoryRepository
    : EfRepositoryBase<Category, CategoryDataModel, int>, ICategoryRepository
{
    public EfCategoryRepository(LibraryDbContext db)
        : base(db, CategoryMapper.ToDomain, CategoryMapper.ToDataModel)
    {
    }

    public async Task<Category?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var dataModel = await Set.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
        return dataModel is null ? null : CategoryMapper.ToDomain(dataModel);
    }

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        return Set.AsNoTracking().AnyAsync(c => c.Name == name, cancellationToken);
    }
}