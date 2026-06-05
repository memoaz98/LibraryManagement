namespace LibraryManagement.Infrastructure.Persistence.DataModels.Catalogs;

/// <summary>
/// Persistence model for the <c>LoanStatus</c> lookup table.
/// Seeded by migration V002 with stable IDs (1-4).
/// </summary>
public class LoanStatusDataModel
{
    public byte Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}