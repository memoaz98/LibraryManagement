using LibraryManagement.Domain.Entities;
using LibraryManagement.Infrastructure.Persistence.DataModels;

namespace LibraryManagement.Infrastructure.Persistence.Mappers;

internal static class MemberMapper
{
    public static Member ToDomain(MemberDataModel dataModel)
    {
        return Member.Reconstitute(
            id: dataModel.Id,
            userId: dataModel.UserId,
            firstName: dataModel.FirstName,
            lastName: dataModel.LastName,
            email: dataModel.Email,
            membershipDate: dataModel.MembershipDate,
            isActive: dataModel.IsActive,
            createdAt: dataModel.CreatedAt,
            isDeleted: dataModel.IsDeleted);
    }

    public static MemberDataModel ToDataModel(Member domain)
    {
        return new MemberDataModel
        {
            Id = domain.Id,
            UserId = domain.UserId,
            FirstName = domain.FirstName,
            LastName = domain.LastName,
            Email = domain.Email,
            MembershipDate = domain.MembershipDate,
            IsActive = domain.IsActive,
            CreatedAt = domain.CreatedAt,
            IsDeleted = domain.IsDeleted
        };
    }
}