namespace LibraryManagement.Domain.Common;

/// <summary>
/// Base class for all domain entities that participate in persistence.
/// </summary>
/// <typeparam name="TId">
/// The type of the entity identifier (int, long, Guid, string, ...).
/// </typeparam>
/// <remarks>
/// Provides three cross-cutting concerns shared by every domain entity:
/// <list type="bullet">
///   <item>A strongly-typed identifier (<see cref="Id"/>).</item>
///   <item>An audit timestamp set at creation (<see cref="CreatedAt"/>).</item>
///   <item>A soft-delete marker (<see cref="IsDeleted"/>) honored by the
///         <c>LibraryDbContext</c> via a global query filter.</item>
/// </list>
/// <para>
/// Lookup tables (CopyStatus, LoanStatus, AspNetRoles) do NOT inherit from
/// this class because they have no soft-delete or audit semantics.
/// </para>
/// </remarks>
public abstract class BaseEntity<TId>
    where TId : notnull
{
    /// <summary>
    /// The primary key of the entity. Assigned by the persistence layer for
    /// IDENTITY-backed entities, or by the factory method for entities whose
    /// IDs are generated client-side.
    /// </summary>
    public TId Id { get; protected set; } = default!;

    /// <summary>
    /// UTC timestamp captured when the entity was first created.
    /// Never modified after the initial save.
    /// </summary>
    public DateTime CreatedAt { get; protected set; }

    /// <summary>
    /// Soft-delete marker. When <c>true</c> the row is hidden from default
    /// queries by the EF Core global query filter, yet remains in the database
    /// for audit and recovery purposes.
    /// </summary>
    public bool IsDeleted { get; protected set; }

    /// <summary>
    /// Marks the entity as deleted without removing the row from the database.
    /// </summary>
    /// <remarks>
    /// Hard deletion is intentionally not exposed on the base type — call sites
    /// that need it must do so explicitly through the repository, bypassing the
    /// query filter via <c>IgnoreQueryFilters()</c>.
    /// </remarks>
    public void MarkAsDeleted()
    {
        IsDeleted = true;
    }
}