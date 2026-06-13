namespace LibraryManagement.Application.Auth;

/// <summary>
/// In-memory representation of a refresh token, used between the
/// application service and the repository implementation.
/// </summary>
/// <remarks>
/// <see cref="TokenHash"/> is the SHA-256 of the raw token. The raw token
/// is shown to the client only once (on creation) and is never persisted
/// or held server-side.
/// </remarks>
public record RefreshTokenModel(
    long Id,
    string UserId,
    byte[] TokenHash,
    DateTime ExpiresAt,
    DateTime CreatedAt,
    DateTime? RevokedAt);