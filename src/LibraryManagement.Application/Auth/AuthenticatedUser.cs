namespace LibraryManagement.Application.Auth;

/// <summary>
/// Identity-provider-agnostic representation of an authenticated user.
/// Used by Application services so they don't depend on Identity
/// implementation details (ASP.NET Identity, Auth0, etc.).
/// </summary>
public record AuthenticatedUser(string Id, string Email);