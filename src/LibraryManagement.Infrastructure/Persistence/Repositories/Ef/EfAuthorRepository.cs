using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;
using LibraryManagement.Infrastructure.Persistence.DataModels;
using LibraryManagement.Infrastructure.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Persistence.Repositories.Ef;

public class EfAuthorRepository
    : EfRepositoryBase<Author, AuthorDataModel, long>, IAuthorRepository
{
    public EfAuthorRepository(LibraryDbContext db)
        : base(db, AuthorMapper.ToDomain, AuthorMapper.ToDataModel)
    {
    }

    public async Task<IReadOnlyList<Author>> FindByLastNameAsync(string lastNamePartial, CancellationToken cancellationToken = default)
    {
        var dataModels = await Set.AsNoTracking()
            .Where(a => EF.Functions.Like(a.LastName, $"%{lastNamePartial}%"))
            .ToListAsync(cancellationToken);
        return dataModels.Select(AuthorMapper.ToDomain).ToList();
    }
}