namespace LibraryManagement.Application.Loans;

/// <summary>
/// Strongly-typed configuration for loans. Bound from the "Loan" section of
/// appsettings.json.
/// </summary>
public class LoanOptions
{
    public const string SectionName = "Loan";

    /// <summary>
    /// Days until a new loan is due. The domain accepts 1 to 90.
    /// </summary>
    public int DefaultLoanDays { get; set; }

    /// <summary>
    /// Maximum number of open (Active or Overdue) loans a member can hold.
    /// </summary>
    public int MaxActiveLoansPerMember { get; set; }
}
