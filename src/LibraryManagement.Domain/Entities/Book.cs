using LibraryManagement.Domain.Common;
using LibraryManagement.Domain.Exceptions;

namespace LibraryManagement.Domain.Entities;

/// <summary>
/// A work in the library catalog. Each <see cref="Book"/> represents the
/// abstract title (e.g. "One Hundred Years of Solitude"), independent of
/// the physical copies the library owns (modeled by <see cref="BookCopy"/>).
/// </summary>
public class Book : BaseEntity<long>
{
    private const int MaxTitleLength = 255;
    private const int MinPublicationYear = 1450;   // ~Gutenberg
    private const int MaxPublicationYearLookahead = 1;   // allow next year for forthcoming books

    public string Title { get; private set; } = default!;
    public string Isbn { get; private set; } = default!;
    public short PublicationYear { get; private set; }
    public string? Synopsis { get; private set; }
    public int CategoryId { get; private set; }

    private Book()
    {
    }

    public static Book Create(
        string title,
        string isbn,
        short publicationYear,
        int categoryId,
        string? synopsis = null)
    {
        ValidateTitle(title);
        ValidateIsbn(isbn);
        ValidatePublicationYear(publicationYear);
        ValidateCategoryId(categoryId);

        return new Book
        {
            Title = title.Trim(),
            Isbn = isbn.Trim(),
            PublicationYear = publicationYear,
            CategoryId = categoryId,
            Synopsis = string.IsNullOrWhiteSpace(synopsis) ? null : synopsis.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public static Book Reconstitute(
        long id,
        string title,
        string isbn,
        short publicationYear,
        int categoryId,
        string? synopsis,
        DateTime createdAt,
        bool isDeleted)
    {
        return new Book
        {
            Id = id,
            Title = title,
            Isbn = isbn,
            PublicationYear = publicationYear,
            CategoryId = categoryId,
            Synopsis = synopsis,
            CreatedAt = createdAt,
            IsDeleted = isDeleted
        };
    }

    public void UpdateTitle(string newTitle)
    {
        ValidateTitle(newTitle);
        Title = newTitle.Trim();
    }

    public void UpdateSynopsis(string? newSynopsis)
    {
        Synopsis = string.IsNullOrWhiteSpace(newSynopsis) ? null : newSynopsis.Trim();
    }

    public void ChangeCategory(int newCategoryId)
    {
        ValidateCategoryId(newCategoryId);
        CategoryId = newCategoryId;
    }

    /// <remarks>
    /// ISBN and publication year are intentionally immutable after creation.
    /// A wrong ISBN means the book was registered as a different title —
    /// the correct fix is to soft-delete and recreate, not edit in place.
    /// </remarks>

    // -----------------------------------------------------------------------
    // Validation
    // -----------------------------------------------------------------------

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Book title is required.");

        if (title.Trim().Length > MaxTitleLength)
            throw new DomainException($"Book title must not exceed {MaxTitleLength} characters.");
    }

    private static void ValidateIsbn(string isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            throw new DomainException("ISBN is required.");

        var trimmed = isbn.Trim();

        if (trimmed.Length != 13 || !trimmed.All(char.IsDigit))
            throw new DomainException("ISBN must consist of exactly 13 digits.");

        if (!IsValidIsbn13Checksum(trimmed))
            throw new DomainException("ISBN-13 checksum is invalid.");
    }

    private static bool IsValidIsbn13Checksum(string isbn13)
    {
        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            var digit = isbn13[i] - '0';
            sum += i % 2 == 0 ? digit : digit * 3;
        }

        var expectedChecksum = (10 - (sum % 10)) % 10;
        var actualChecksum = isbn13[12] - '0';
        return expectedChecksum == actualChecksum;
    }

    private static void ValidatePublicationYear(short year)
    {
        var maxAllowed = DateTime.UtcNow.Year + MaxPublicationYearLookahead;

        if (year < MinPublicationYear || year > maxAllowed)
            throw new DomainException(
                $"Publication year must be between {MinPublicationYear} and {maxAllowed}.");
    }

    private static void ValidateCategoryId(int categoryId)
    {
        if (categoryId <= 0)
            throw new DomainException("CategoryId must be a positive integer.");
    }
}