namespace LibraryManagement.Application.Common.Exceptions;

/// <summary>
/// Thrown when authentication credentials are invalid (wrong password,
/// expired token, revoked token, missing claims). The WebApi translates
/// this to HTTP 401 Unauthorized.
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}