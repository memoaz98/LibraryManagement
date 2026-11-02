namespace LibraryManagement.Application.Loans;

/// <summary>
/// Reads the <c>status</c> query string value of the loan lists. Matching is
/// case-insensitive, and no value means every loan.
/// </summary>
public static class LoanStatusFilterParser
{
    public static bool TryParse(string? value, out LoanStatusFilter filter)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case null or "":
                filter = LoanStatusFilter.All;
                return true;
            case "active":
                filter = LoanStatusFilter.Active;
                return true;
            case "returned":
                filter = LoanStatusFilter.Returned;
                return true;
            default:
                filter = LoanStatusFilter.All;
                return false;
        }
    }

    /// <summary>
    /// Parses a value that has already been validated.
    /// </summary>
    public static LoanStatusFilter Parse(string? value)
    {
        return TryParse(value, out var filter)
            ? filter
            : throw new ArgumentException($"Unknown loan status filter '{value}'.", nameof(value));
    }
}
