using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LibraryManagement.Application.Auth;
using LibraryManagement.Application.Auth.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LibraryManagement.Infrastructure.Auth;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _options;

    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public AccessTokenResult GenerateAccessToken(AuthenticatedUser user, IList<string> roles)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim("role", role));
        }

        var keyBytes = Convert.FromBase64String(_options.SigningKey);
        var signingKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var handler = new JwtSecurityTokenHandler();
        handler.OutboundClaimTypeMap.Clear();

        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString = handler.WriteToken(jwt);

        return new AccessTokenResult(tokenString, expiresAt);
    }
}