namespace LibraryManagement.Domain.Constants;

/// <summary>
/// Stable identifiers for the <c>CopyStatus</c> catalog table.
/// </summary>
/// <remarks>
/// These values are seeded by migration V002 and must never be changed
/// without a corresponding data migration. The numeric values are part of
/// the contract between database and code.
/// </remarks>
public static class CopyStatusIds
{
    public const byte Available   = 1;
    public const byte Borrowed    = 2;
    public const byte Maintenance = 3;
    public const byte Lost        = 4;
}

/// <summary>
/// Stable identifiers for the <c>LoanStatus</c> catalog table.
/// </summary>
/// <remarks>
/// Seeded by migration V002. See <see cref="CopyStatusIds"/> for stability notes.
/// </remarks>
public static class LoanStatusIds
{
    public const byte Active   = 1;
    public const byte Returned = 2;
    public const byte Overdue  = 3;
    public const byte Lost     = 4;
}

/// <summary>
/// Stable identifiers for the seeded ASP.NET Identity roles.
/// </summary>
/// <remarks>
/// The GUID strings match those inserted in migration V002. Use the
/// <see cref="Names"/> nested class when registering authorization policies
/// or applying <c>[Authorize(Roles = ...)]</c> attributes (which expect
/// role names, not ids).
/// </remarks>
public static class RoleIds
{
    public const string Administrator = "a0000000-0000-0000-0000-000000000001";
    public const string Librarian     = "a0000000-0000-0000-0000-000000000002";
    public const string Reader        = "a0000000-0000-0000-0000-000000000003";

    public static class Names
    {
        public const string Administrator = "Administrator";
        public const string Librarian     = "Librarian";
        public const string Reader        = "Reader";
    }
}