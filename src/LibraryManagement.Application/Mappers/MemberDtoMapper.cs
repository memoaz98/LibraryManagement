using LibraryManagement.Application.Dtos.Members;
using LibraryManagement.Domain.Entities;

namespace LibraryManagement.Application.Mappers;

internal static class MemberDtoMapper
{
    public static MemberDto ToDto(Member domain)
    {
        return new MemberDto(
            Id: domain.Id,
            FirstName: domain.FirstName,
            LastName: domain.LastName,
            Email: domain.Email,
            IsActive: domain.IsActive);
    }
}
