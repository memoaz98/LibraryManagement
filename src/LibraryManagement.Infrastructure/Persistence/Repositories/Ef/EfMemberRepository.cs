using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;
using LibraryManagement.Infrastructure.Persistence.DataModels;
using LibraryManagement.Infrastructure.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Persistence.Repositories.Ef;

public class EfMemberRepository
    : EfRepositoryBase<Member, MemberDataModel, long>, IMemberRepository
{
    public EfMemberRepository(LibraryDbContext db)
        : base(db, MemberMapper.ToDomain, MemberMapper.ToDataModel)
    {
    }

    public async Task<Member?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var dataModel = await Set.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Email == normalized, cancellationToken);
        return dataModel is null ? null : MemberMapper.ToDomain(dataModel);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return Set.AsNoTracking().AnyAsync(m => m.Email == normalized, cancellationToken);
    }

    public async Task<Member?> FindByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var dataModel = await Set.AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId, cancellationToken);
        return dataModel is null ? null : MemberMapper.ToDomain(dataModel);
    }
}