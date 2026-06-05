using LibraryManagement.Infrastructure.Persistence.DataModels.Catalogs;

namespace LibraryManagement.Infrastructure.Persistence.DataModels;

public class LoanDataModel
{
    public long Id { get; set; }
    public long BookCopyId { get; set; }
    public long MemberId { get; set; }
    public byte StatusId { get; set; }
    public DateOnly LoanDate { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly? ReturnDate { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual BookCopyDataModel BookCopy { get; set; } = default!;
    public virtual MemberDataModel Member { get; set; } = default!;
    public virtual LoanStatusDataModel Status { get; set; } = default!;
}