using LibraryManagement.Domain.Common;
using LibraryManagement.Domain.Exceptions;

namespace LibraryManagement.Domain.Entities;

/// <summary>
/// A classification a <see cref="Book"/> belongs to (e.g. Fiction, History,
/// Computer Science).
/// </summary>
public class Category : BaseEntity<int>
{
    private const int MaxNameLength = 100;
    private const int MaxDescriptionLength = 500;

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    /// <summary>
    /// Required by ORM reconstitution; not intended for direct use.
    /// </summary>
    private Category()
    {
    }

    /// <summary>
    /// Factory for creating a brand-new <see cref="Category"/>.
    /// Validates all domain invariants before returning the instance.
    /// </summary>
    public static Category Create(string name, string? description = null)
    {
        ValidateName(name);
        ValidateDescription(description);

        return new Category
        {
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    /// <summary>
    /// Reconstructs a <see cref="Category"/> from persisted state.
    /// Intended for the mapping layer; bypasses validation because the data
    /// is assumed to come from a trusted persistence source.
    /// </summary>
    public static Category Reconstitute(
        int id,
        string name,
        string? description,
        DateTime createdAt,
        bool isDeleted)
    {
        return new Category
        {
            Id = id,
            Name = name,
            Description = description,
            CreatedAt = createdAt,
            IsDeleted = isDeleted
        };
    }

    public void Rename(string newName)
    {
        ValidateName(newName);
        Name = newName.Trim();
    }

    public void UpdateDescription(string? newDescription)
    {
        ValidateDescription(newDescription);
        Description = string.IsNullOrWhiteSpace(newDescription) ? null : newDescription.Trim();
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Category name is required.");

        if (name.Trim().Length > MaxNameLength)
            throw new DomainException($"Category name must not exceed {MaxNameLength} characters.");
    }

    private static void ValidateDescription(string? description)
    {
        if (description is not null && description.Length > MaxDescriptionLength)
            throw new DomainException($"Category description must not exceed {MaxDescriptionLength} characters.");
    }
}