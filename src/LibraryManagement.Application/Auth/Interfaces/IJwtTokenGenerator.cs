namespace LibraryManagement.Application.Auth.Interfaces;

/// <summary>
/// Contract for generating signed JWT access tokens for authenticated users.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Builds and signs an access token for the given user with the
    /// specified roles embedded as claims.
    /// </summary>
    AccessTokenResult GenerateAccessToken(AuthenticatedUser user, IList<string> roles);
}