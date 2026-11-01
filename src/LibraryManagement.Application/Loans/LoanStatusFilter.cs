namespace LibraryManagement.Application.Loans;

/// <summary>
/// Which loans a list query returns. Parsed from the <c>status</c> query
/// string parameter.
/// </summary>
public enum LoanStatusFilter
{
    /// <summary>Every loan (no <c>status</c> given).</summary>
    All,

    /// <summary>Open loans: Active or Overdue (<c>status=active</c>).</summary>
    Active,

    /// <summary>Returned loans (<c>status=returned</c>).</summary>
    Returned
}
