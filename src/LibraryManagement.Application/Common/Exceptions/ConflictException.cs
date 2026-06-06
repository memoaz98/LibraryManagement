namespace LibraryManagement.Application.Common.Exceptions;

/// <summary>
/// Thrown when a request conflicts with the current state of the system.
/// Typically a duplicate (unique value already taken) or a stale-state
/// situation (trying to mutate an entity in an incompatible state).
/// </summary>
/// <remarks>
/// The WebApi layer translates this to HTTP 409 Conflict via the global
/// exception handler.
/// </remarks>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}