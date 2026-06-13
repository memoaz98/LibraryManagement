namespace LibraryManagement.Application.Dtos.Auth;

/// <summary>
/// Payload for POST /auth/register.
/// </summary>
public record RegisterDto(string Email, string Password);