using LibraryManagement.Domain.Entities;
using LibraryManagement.Infrastructure.Persistence.DataModels;

namespace LibraryManagement.Infrastructure.Persistence.Mappers;

internal static class LoanMapper
{
    public static Loan ToDomain(LoanDataModel dataModel)
    {
        return Loan.Reconstitute(
            id: dataModel.Id,
            bookCopyId: dataModel.BookCopyId,
            memberId: dataModel.MemberId,
            statusId: dataModel.StatusId,
            loanDate: dataModel.LoanDate,
            dueDate: dataModel.DueDate,
            returnDate: dataModel.ReturnDate,
            createdAt: dataModel.CreatedAt,
            isDeleted: dataModel.IsDeleted);
    }

    public static LoanDataModel ToDataModel(Loan domain)
    {
        return new LoanDataModel
        {
            Id = domain.Id,
            BookCopyId = domain.BookCopyId,
            MemberId = domain.MemberId,
            StatusId = domain.StatusId,
            LoanDate = domain.LoanDate,
            DueDate = domain.DueDate,
            ReturnDate = domain.ReturnDate,
            CreatedAt = domain.CreatedAt,
            IsDeleted = domain.IsDeleted
        };
    }
}