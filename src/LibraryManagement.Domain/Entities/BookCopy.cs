using LibraryManagement.Domain.Common;
using LibraryManagement.Domain.Constants;
using LibraryManagement.Domain.Exceptions;

namespace LibraryManagement.Domain.Entities;

/// <summary>
/// A physical copy of a <see cref="Book"/>. The library may own many copies
/// of the same title; loans target copies, not books.
/// </summary>
/// <remarks>
/// State transitions are constrained: see the static graph on the project
/// architecture docs. Invalid transitions throw <see cref="DomainException"/>.
/// </remarks>
public class BookCopy : BaseEntity<long>
{
    private const int MaxInventoryCodeLength = 20;

    public long BookId { get; private set; }
    public byte StatusId { get; private set; }
    public string InventoryCode { get; private set; } = default!;
    public DateOnly AcquisitionDate { get; private set; }

    private BookCopy()
    {
    }

    public static BookCopy Create(long bookId, string inventoryCode, DateOnly acquisitionDate)
    {
        ValidateBookId(bookId);
        ValidateInventoryCode(inventoryCode);
        ValidateAcquisitionDate(acquisitionDate);

        return new BookCopy
        {
            BookId = bookId,
            InventoryCode = inventoryCode.Trim(),
            AcquisitionDate = acquisitionDate,
            StatusId = CopyStatusIds.Available,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public static BookCopy Reconstitute(
        long id,
        long bookId,
        byte statusId,
        string inventoryCode,
        DateOnly acquisitionDate,
        DateTime createdAt,
        bool isDeleted)
    {
        return new BookCopy
        {
            Id = id,
            BookId = bookId,
            StatusId = statusId,
            InventoryCode = inventoryCode,
            AcquisitionDate = acquisitionDate,
            CreatedAt = createdAt,
            IsDeleted = isDeleted
        };
    }

    // -----------------------------------------------------------------------
    // State transitions
    // -----------------------------------------------------------------------

    /// <summary>
    /// Transitions the copy from Available to Borrowed. Called when a Loan
    /// is registered.
    /// </summary>
    public void MarkAsBorrowed()
    {
        EnsureCurrentStatusIs(CopyStatusIds.Available, "lend");
        StatusId = CopyStatusIds.Borrowed;
    }

    /// <summary>
    /// Transitions the copy from Borrowed back to Available. Called when a
    /// Loan is returned.
    /// </summary>
    public void MarkAsReturned()
    {
        EnsureCurrentStatusIs(CopyStatusIds.Borrowed, "return");
        StatusId = CopyStatusIds.Available;
    }

    /// <summary>
    /// Transitions the copy from Available to Maintenance.
    /// </summary>
    public void SendToMaintenance()
    {
        EnsureCurrentStatusIs(CopyStatusIds.Available, "send to maintenance");
        StatusId = CopyStatusIds.Maintenance;
    }

    /// <summary>
    /// Transitions the copy from Maintenance back to Available.
    /// </summary>
    public void ReturnFromMaintenance()
    {
        EnsureCurrentStatusIs(CopyStatusIds.Maintenance, "return from maintenance");
        StatusId = CopyStatusIds.Available;
    }

    /// <summary>
    /// Transitions the copy to the terminal Lost state. Allowed from Borrowed
    /// or Maintenance, not from Available (a copy on the shelf cannot be lost
    /// without first being borrowed or sent to maintenance).
    /// </summary>
    public void MarkAsLost()
    {
        if (StatusId is not CopyStatusIds.Borrowed and not CopyStatusIds.Maintenance)
            throw new DomainException(
                "A copy can only be marked as lost from Borrowed or Maintenance state.");

        StatusId = CopyStatusIds.Lost;
    }

    // -----------------------------------------------------------------------
    // Validation helpers
    // -----------------------------------------------------------------------

    private void EnsureCurrentStatusIs(byte expectedStatusId, string actionDescription)
    {
        if (StatusId != expectedStatusId)
            throw new DomainException(
                $"Cannot {actionDescription} a copy whose status is not the required prerequisite.");
    }

    private static void ValidateBookId(long bookId)
    {
        if (bookId <= 0)
            throw new DomainException("BookId must be a positive integer.");
    }

    private static void ValidateInventoryCode(string inventoryCode)
    {
        if (string.IsNullOrWhiteSpace(inventoryCode))
            throw new DomainException("Inventory code is required.");

        if (inventoryCode.Trim().Length > MaxInventoryCodeLength)
            throw new DomainException(
                $"Inventory code must not exceed {MaxInventoryCodeLength} characters.");
    }

    private static void ValidateAcquisitionDate(DateOnly acquisitionDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (acquisitionDate > today)
            throw new DomainException("Acquisition date cannot be in the future.");
    }
}