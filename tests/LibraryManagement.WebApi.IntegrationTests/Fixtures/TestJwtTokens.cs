using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LibraryManagement.Application.Auth;
using Microsoft.IdentityModel.Tokens;

namespace LibraryManagement.WebApi.IntegrationTests.Fixtures;

/// <summary>
/// Mints access tokens the API accepts, for any user id and roles, without a
/// login round trip. The user does not need to exist in AspNetUsers.
/// </summary>
internal static class TestJwtTokens
{
    public static string Create(JwtOptions options, string userId, IEnumerable<string> roles)
    {
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, userId) };
        claims.AddRange(roles.Select(role => new Claim("role", role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Convert.FromBase64String(options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        // No "nbf", and an expiry far from both the real and the fake clock:
        // token validation may read either, and tests move the fake clock.
        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: null,
            expires: DateTime.UtcNow.AddYears(10),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
