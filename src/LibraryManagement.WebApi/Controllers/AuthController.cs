using LibraryManagement.Application.Dtos.Auth;
using LibraryManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.WebApi.Controllers;

/// <summary>
/// HTTP endpoints for authentication: register, login, refresh, logout.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponseDto>> RegisterAsync(
        [FromBody] RegisterDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(dto.Email, dto.Password, cancellationToken);
        return Ok(MapToResponse(result));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> LoginAsync(
        [FromBody] LoginDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(dto.Email, dto.Password, cancellationToken);
        return Ok(MapToResponse(result));
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> RefreshAsync(
        [FromBody] RefreshTokenDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshAsync(dto.RefreshToken, cancellationToken);
        return Ok(MapToResponse(result));
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAsync(
        [FromBody] RefreshTokenDto dto,
        CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(dto.RefreshToken, cancellationToken);
        return NoContent();
    }

    private static AuthResponseDto MapToResponse(LibraryManagement.Application.Auth.AuthResult result)
    {
        return new AuthResponseDto(
            AccessToken: result.AccessToken,
            AccessTokenExpiresAt: result.AccessTokenExpiresAt,
            RefreshToken: result.RefreshToken,
            RefreshTokenExpiresAt: result.RefreshTokenExpiresAt);
    }
}