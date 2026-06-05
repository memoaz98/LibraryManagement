namespace LibraryManagement.Infrastructure.Persistence.DataModels;

public class BookDataModel
{
    public long Id { get; set; }
    public int CategoryId { get; set; }
    public string Title { get; set; } = default!;
    public string Isbn { get; set; } = default!;
    public short PublicationYear { get; set; }
    public string? Synopsis { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual CategoryDataModel Category { get; set; } = default!;
    public virtual ICollection<AuthorDataModel> Authors { get; set; } = new List<AuthorDataModel>();
    public virtual ICollection<BookCopyDataModel> Copies { get; set; } = new List<BookCopyDataModel>();
}