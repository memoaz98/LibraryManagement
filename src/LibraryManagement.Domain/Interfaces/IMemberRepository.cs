using LibraryManagement.Domain.Entities;

namespace LibraryManagement.Domain.Interfaces;

public interface IMemberRepository : IRepository<Member, long>
{
    Task<Member?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<Member?> FindByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}