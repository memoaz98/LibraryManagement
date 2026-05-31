namespace LibraryManagement.Domain.Exceptions;

/// <summary>
/// Represents a violation of a business rule from the library domain.
/// </summary>
/// <remarks>
/// Raised when an operation would leave a domain entity in an invalid state
/// (e.g. lending a copy that is already borrowed, creating a loan with a
/// due date before the loan date).
/// <para>
/// At the HTTP boundary this exception is translated to a 400 Bad Request
/// by the global exception handler.
/// </para>
/// </remarks>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}