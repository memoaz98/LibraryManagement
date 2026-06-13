namespace LibraryManagement.Application.Auth;

/// <summary>
/// Strongly-typed configuration for JWT generation. Bound from the
/// "Jwt" section of appsettings.json + user secrets.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// The issuer of the token (typically the API name). Embedded as the
    /// <c>iss</c> claim and validated on incoming tokens.
    /// </summary>
    public string Issuer { get; set; } = default!;

    /// <summary>
    /// The intended audience of the token (typically the client name).
    /// Embedded as the <c>aud</c> claim and validated on incoming tokens.
    /// </summary>
    public string Audience { get; set; } = default!;

    /// <summary>
    /// Lifetime of access tokens, in minutes.
    /// </summary>
    public int AccessTokenMinutes { get; set; }

    /// <summary>
    /// Lifetime of refresh tokens, in days.
    /// </summary>
    public int RefreshTokenDays { get; set; }

    /// <summary>
    /// The secret key used to sign tokens with HMAC-SHA256.
    /// Must be at least 32 bytes (256 bits) once Base64-decoded.
    /// Stored in user secrets / environment variables — never in source.
    /// </summary>
    public string SigningKey { get; set; } = default!;
}