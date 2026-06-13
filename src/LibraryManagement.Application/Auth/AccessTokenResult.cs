namespace LibraryManagement.Application.Auth;

/// <summary>
/// Result of generating an access token: the JWT itself and the absolute
/// time when it expires.
/// </summary>
public record AccessTokenResult(string Token, DateTime ExpiresAt);