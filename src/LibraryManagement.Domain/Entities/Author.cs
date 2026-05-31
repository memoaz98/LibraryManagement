using LibraryManagement.Domain.Common;
using LibraryManagement.Domain.Exceptions;

namespace LibraryManagement.Domain.Entities;

/// <summary>
/// A person who wrote one or more <see cref="Book"/>s.
/// </summary>
/// <remarks>
/// Books and authors are related many-to-many through the
/// <c>BookAuthors</c> junction table.
/// </remarks>
public class Author : BaseEntity<long>
{
    private const int MaxNameLength = 100;

    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public DateOnly? DateOfBirth { get; private set; }
    public string? Biography { get; private set; }

    /// <summary>
    /// Convenience read-only projection. Not persisted; derived from
    /// <see cref="FirstName"/> and <see cref="LastName"/>.
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";

    private Author()
    {
    }

    public static Author Create(
        string firstName,
        string lastName,
        DateOnly? dateOfBirth = null,
        string? biography = null)
    {
        ValidateName(firstName, nameof(firstName));
        ValidateName(lastName, nameof(lastName));
        ValidateDateOfBirth(dateOfBirth);

        return new Author
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            DateOfBirth = dateOfBirth,
            Biography = string.IsNullOrWhiteSpace(biography) ? null : biography.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public static Author Reconstitute(
        long id,
        string firstName,
        string lastName,
        DateOnly? dateOfBirth,
        string? biography,
        DateTime createdAt,
        bool isDeleted)
    {
        return new Author
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = dateOfBirth,
            Biography = biography,
            CreatedAt = createdAt,
            IsDeleted = isDeleted
        };
    }

    public void Rename(string firstName, string lastName)
    {
        ValidateName(firstName, nameof(firstName));
        ValidateName(lastName, nameof(lastName));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }

    public void UpdateBiography(string? biography)
    {
        Biography = string.IsNullOrWhiteSpace(biography) ? null : biography.Trim();
    }

    public void UpdateDateOfBirth(DateOnly? dateOfBirth)
    {
        ValidateDateOfBirth(dateOfBirth);
        DateOfBirth = dateOfBirth;
    }

    private static void ValidateName(string name, string paramName)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException($"{paramName} is required.");

        if (name.Trim().Length > MaxNameLength)
            throw new DomainException($"{paramName} must not exceed {MaxNameLength} characters.");
    }

    private static void ValidateDateOfBirth(DateOnly? dateOfBirth)
    {
        if (dateOfBirth is null) return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (dateOfBirth.Value > today)
            throw new DomainException("Date of birth cannot be in the future.");
    }
}