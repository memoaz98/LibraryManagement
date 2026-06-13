namespace LibraryManagement.Application.Dtos.Auth;

/// <summary>
/// Payload for POST /auth/login.
/// </summary>
public record LoginDto(string Email, string Password);