using LibraryManagement.Infrastructure.Persistence.DataModels.Catalogs;

namespace LibraryManagement.Infrastructure.Persistence.DataModels;

public class BookCopyDataModel
{
    public long Id { get; set; }
    public long BookId { get; set; }
    public byte StatusId { get; set; }
    public string InventoryCode { get; set; } = default!;
    public DateOnly AcquisitionDate { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual BookDataModel Book { get; set; } = default!;
    public virtual CopyStatusDataModel Status { get; set; } = default!;
    public virtual ICollection<LoanDataModel> Loans { get; set; } = new List<LoanDataModel>();
}