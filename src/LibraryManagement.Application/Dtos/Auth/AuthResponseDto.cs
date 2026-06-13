namespace LibraryManagement.Application.Dtos.Auth;

/// <summary>
/// Response for register, login, and refresh: the two tokens plus their
/// expiration timestamps (UTC).
/// </summary>
public record AuthResponseDto(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);