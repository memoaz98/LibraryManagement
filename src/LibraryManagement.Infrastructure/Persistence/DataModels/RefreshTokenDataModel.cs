using Microsoft.AspNetCore.Identity;

namespace LibraryManagement.Infrastructure.Persistence.DataModels;

/// <summary>
/// Persistence model for refresh tokens. Stores a SHA-256 HASH of the token,
/// never the raw value (defense in depth — if the BD is compromised, tokens
/// are not directly usable).
/// </summary>
public class RefreshTokenDataModel
{
    public long Id { get; set; }
    public string UserId { get; set; } = default!;
    public byte[] TokenHash { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public virtual IdentityUser User { get; set; } = default!;
}