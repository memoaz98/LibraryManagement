using Microsoft.AspNetCore.Identity;

namespace LibraryManagement.Infrastructure.Persistence.DataModels;

public class MemberDataModel
{
    public long Id { get; set; }
    public string? UserId { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public DateOnly MembershipDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual IdentityUser? User { get; set; }
    public virtual ICollection<LoanDataModel> Loans { get; set; } = new List<LoanDataModel>();
}