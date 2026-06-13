using System.Security.Cryptography;
using System.Text;
using LibraryManagement.Application.Auth;
using LibraryManagement.Application.Auth.Interfaces;
using LibraryManagement.Application.Common.Exceptions;
using LibraryManagement.Domain.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LibraryManagement.Application.Services;

/// <summary>
/// Coordinates the authentication use cases: register, login, refresh, logout.
/// Combines ASP.NET Identity (for user/password/role management) with the
/// JWT generator (for access tokens) and the refresh token repository (for
/// persisted refresh tokens).
/// </summary>
public class AuthService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        UserManager<IdentityUser> userManager,
        IJwtTokenGenerator jwtGenerator,
        IRefreshTokenRepository refreshTokenRepo,
        IOptions<JwtOptions> jwtOptions)
    {
        _userManager = userManager;
        _jwtGenerator = jwtGenerator;
        _refreshTokenRepo = refreshTokenRepo;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResult> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            throw new ConflictException($"A user with email '{email}' already exists.");
        }

        var user = new IdentityUser
        {
            UserName = email,
            Email = email
        };

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            throw new ConflictException($"User creation failed: {errors}");
        }

        await _userManager.AddToRoleAsync(user, RoleIds.Names.Reader);

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResult> RefreshAsync(
        string refreshTokenPlain,
        CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(refreshTokenPlain);
        var existingToken = await _refreshTokenRepo.FindActiveByHashAsync(tokenHash, cancellationToken);

        if (existingToken is null)
        {
            throw new UnauthorizedException("Refresh token is invalid, revoked, or expired.");
        }

        var user = await _userManager.FindByIdAsync(existingToken.UserId);
        if (user is null)
        {
            throw new UnauthorizedException("Refresh token is invalid, revoked, or expired.");
        }

        await _refreshTokenRepo.RevokeAsync(existingToken.Id, cancellationToken);

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task LogoutAsync(
        string refreshTokenPlain,
        CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(refreshTokenPlain);
        var existingToken = await _refreshTokenRepo.FindActiveByHashAsync(tokenHash, cancellationToken);

        if (existingToken is null)
        {
            return;
        }

        await _refreshTokenRepo.RevokeAsync(existingToken.Id, cancellationToken);
    }

    private async Task<AuthResult> IssueTokensAsync(
        IdentityUser user,
        CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var authUser = new AuthenticatedUser(user.Id, user.Email ?? string.Empty);
        var accessToken = _jwtGenerator.GenerateAccessToken(authUser, roles);

        var (refreshTokenPlain, refreshTokenHash) = GenerateRefreshToken();
        var refreshExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays);

        await _refreshTokenRepo.AddAsync(
            new RefreshTokenModel(
                Id: 0,
                UserId: user.Id,
                TokenHash: refreshTokenHash,
                ExpiresAt: refreshExpiresAt,
                CreatedAt: DateTime.UtcNow,
                RevokedAt: null),
            cancellationToken);

        return new AuthResult(
            AccessToken: accessToken.Token,
            AccessTokenExpiresAt: accessToken.ExpiresAt,
            RefreshToken: refreshTokenPlain,
            RefreshTokenExpiresAt: refreshExpiresAt);
    }

    private static (string Plain, byte[] Hash) GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var plain = Convert.ToBase64String(randomBytes);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(plain));
        return (plain, hash);
    }

    private static byte[] HashToken(string token)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(token));
    }
}