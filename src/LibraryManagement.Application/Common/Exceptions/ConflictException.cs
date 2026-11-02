namespace LibraryManagement.Application.Common.Exceptions;

/// <summary>
/// Thrown when a request conflicts with the current state of the system.
/// Typically a duplicate (unique value already taken) or a stale-state
/// situation (trying to mutate an entity in an incompatible state).
/// </summary>
/// <remarks>
/// The WebApi layer translates this to HTTP 409 Conflict via the global
/// exception handler. When <see cref="Code"/> is set, the response carries it
/// as a <c>code</c> field so clients can tell the reasons apart.
/// </remarks>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }

    public ConflictException(string message, string code) : base(message)
    {
        Code = code;
    }

    public ConflictException(string message, string? code, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }

    /// <summary>
    /// Stable, machine-readable reason for the conflict, or null when the
    /// message is enough.
    /// </summary>
    public string? Code { get; }
}
