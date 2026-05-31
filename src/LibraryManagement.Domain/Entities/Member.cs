using System.Text.RegularExpressions;
using LibraryManagement.Domain.Common;
using LibraryManagement.Domain.Exceptions;

namespace LibraryManagement.Domain.Entities;

/// <summary>
/// A library patron. Holds the domain concept of "member" — distinct from the
/// authentication account stored in <c>AspNetUsers</c>.
/// </summary>
/// <remarks>
/// A member may optionally be linked to an Identity user via
/// <see cref="UserId"/>. Members can exist without a User (e.g. registered
/// in person, login created later) and can also be deactivated temporarily
/// without being deleted (e.g. suspended for unpaid fines).
/// </remarks>
public class Member : BaseEntity<long>
{
    private const int MaxNameLength = 100;
    private const int MaxEmailLength = 256;

    // Simple, pragmatic email shape check. Defense in depth — a confirmation
    // email is the only real proof of validity.
    private static readonly Regex EmailPattern =
        new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);

    public string? UserId { get; private set; }
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public DateOnly MembershipDate { get; private set; }
    public bool IsActive { get; private set; }

    public string FullName => $"{FirstName} {LastName}";

    private Member()
    {
    }

    public static Member Create(string firstName, string lastName, string email)
    {
        ValidateName(firstName, nameof(firstName));
        ValidateName(lastName, nameof(lastName));
        ValidateEmail(email);

        return new Member
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            MembershipDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsActive = true,
            UserId = null,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public static Member Reconstitute(
        long id,
        string? userId,
        string firstName,
        string lastName,
        string email,
        DateOnly membershipDate,
        bool isActive,
        DateTime createdAt,
        bool isDeleted)
    {
        return new Member
        {
            Id = id,
            UserId = userId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            MembershipDate = membershipDate,
            IsActive = isActive,
            CreatedAt = createdAt,
            IsDeleted = isDeleted
        };
    }

    // -----------------------------------------------------------------------
    // Profile updates
    // -----------------------------------------------------------------------

    public void Rename(string firstName, string lastName)
    {
        ValidateName(firstName, nameof(firstName));
        ValidateName(lastName, nameof(lastName));
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }

    public void ChangeEmail(string newEmail)
    {
        ValidateEmail(newEmail);
        Email = newEmail.Trim().ToLowerInvariant();
    }

    // -----------------------------------------------------------------------
    // Activation lifecycle
    // -----------------------------------------------------------------------

    /// <summary>
    /// Re-enables a previously deactivated member.
    /// Idempotent: activating an already active member is a no-op.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Suspends the member without deleting the record. Deactivated members
    /// cannot take new loans, but their history remains visible.
    /// </summary>
    /// <remarks>
    /// Distinct from soft delete: deactivation is reversible business state,
    /// soft delete is administrative removal.
    /// </remarks>
    public void Deactivate()
    {
        IsActive = false;
    }

    // -----------------------------------------------------------------------
    // Identity link
    // -----------------------------------------------------------------------

    /// <summary>
    /// Associates this member with an Identity user account (AspNetUsers.Id).
    /// </summary>
    public void LinkToUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("UserId is required to link a member to an account.");

        UserId = userId;
    }

    /// <summary>
    /// Removes the Identity association without affecting the member record.
    /// Useful when the underlying account is closed but the member must
    /// remain for historical reasons.
    /// </summary>
    public void UnlinkFromUser()
    {
        UserId = null;
    }

    // -----------------------------------------------------------------------
    // Validation
    // -----------------------------------------------------------------------

    private static void ValidateName(string name, string paramName)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException($"{paramName} is required.");

        if (name.Trim().Length > MaxNameLength)
            throw new DomainException($"{paramName} must not exceed {MaxNameLength} characters.");
    }

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");

        var trimmed = email.Trim();

        if (trimmed.Length > MaxEmailLength)
            throw new DomainException($"Email must not exceed {MaxEmailLength} characters.");

        if (!EmailPattern.IsMatch(trimmed))
            throw new DomainException("Email format is invalid.");
    }
}