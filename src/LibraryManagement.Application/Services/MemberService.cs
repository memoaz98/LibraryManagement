using LibraryManagement.Application.Dtos.Members;
using LibraryManagement.Domain.Interfaces;

namespace LibraryManagement.Application.Services;

/// <summary>
/// Member lookups for the checkout form.
/// </summary>
public class MemberService
{
    private readonly IMemberRepository _memberRepo;

    public MemberService(IMemberRepository memberRepo)
    {
        _memberRepo = memberRepo;
    }

    /// <summary>
    /// Searches members by first name, last name or email. Inactive members
    /// are included; no search term returns every member.
    /// </summary>
    public async Task<IReadOnlyList<MemberDto>> SearchAsync(string? search, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }
}
