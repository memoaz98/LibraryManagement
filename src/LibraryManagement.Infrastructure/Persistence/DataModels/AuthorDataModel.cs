namespace LibraryManagement.Infrastructure.Persistence.DataModels;

public class AuthorDataModel
{
    public long Id { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public DateOnly? DateOfBirth { get; set; }
    public string? Biography { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual ICollection<BookDataModel> Books { get; set; } = new List<BookDataModel>();
}