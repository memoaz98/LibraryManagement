namespace LibraryManagement.Application.Dtos.Auth;

/// <summary>
/// Payload for POST /auth/refresh and POST /auth/logout. Carries only the
/// refresh token; the user is identified by the token itself.
/// </summary>
public record RefreshTokenDto(string RefreshToken);