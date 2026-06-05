namespace LibraryManagement.Infrastructure.Persistence.DataModels.Catalogs;

/// <summary>
/// Persistence model for the <c>CopyStatus</c> lookup table.
/// Seeded by migration V002 with stable IDs (1-4).
/// </summary>
/// <remarks>
/// Lookup tables are not mirrored by domain entities. The domain references
/// them only through stable IDs defined in <c>CopyStatusIds</c>.
/// </remarks>
public class CopyStatusDataModel
{
    public byte Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}