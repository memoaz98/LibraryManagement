namespace LibraryManagement.Infrastructure.Persistence.DataModels;

/// <summary>
/// Persistence model for the <c>Categories</c> table. Mirrors the
/// <see cref="LibraryManagement.Domain.Entities.Category"/> domain entity
/// without business rules or invariants.
/// </summary>
public class CategoryDataModel
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties (populated by EF Core when included)
    public virtual ICollection<BookDataModel> Books { get; set; } = new List<BookDataModel>();
}