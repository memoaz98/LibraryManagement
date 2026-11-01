using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LibraryManagement.Application.Common.Exceptions;
using LibraryManagement.Domain.Constants;

namespace LibraryManagement.WebApi.Extensions;

/// <summary>
/// Reads the caller's identity from the access token. Controllers pass these
/// values to the Application services, which never see the HttpContext.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedException("The access token has no subject claim.");
    }

    public static bool IsLibrarianOrAdmin(this ClaimsPrincipal user)
    {
        return user.IsInRole(RoleIds.Names.Librarian)
            || user.IsInRole(RoleIds.Names.Administrator);
    }
}
