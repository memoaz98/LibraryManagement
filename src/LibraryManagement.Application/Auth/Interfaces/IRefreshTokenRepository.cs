using LibraryManagement.Application.Auth;

namespace LibraryManagement.Application.Auth.Interfaces;

/// <summary>
/// Contract for persisting and querying refresh tokens.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Inserts a new refresh token. Returns the assigned Id.
    /// </summary>
    Task<long> AddAsync(RefreshTokenModel token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an active (non-revoked, non-expired) refresh token by its hash.
    /// Returns null if not found or already revoked/expired.
    /// </summary>
    Task<RefreshTokenModel?> FindActiveByHashAsync(byte[] tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a refresh token as revoked.
    /// </summary>
    Task RevokeAsync(long tokenId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all active refresh tokens for a user (e.g. logout-from-all,
    /// or detected token reuse).
    /// </summary>
    Task RevokeAllForUserAsync(string userId, CancellationToken cancellationToken = default);
}