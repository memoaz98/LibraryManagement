namespace LibraryManagement.Application.Auth;

/// <summary>
/// Result of a successful authentication operation: access + refresh tokens
/// with their expiration times.
/// </summary>
public record AuthResult(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);