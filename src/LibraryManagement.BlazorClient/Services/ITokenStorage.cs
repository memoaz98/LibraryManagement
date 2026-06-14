namespace LibraryManagement.BlazorClient.Services;

/// <summary>
/// Abstraction over where tokens are persisted on the client.
/// Implementations may use localStorage, sessionStorage, or memory.
/// </summary>
public interface ITokenStorage
{
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task SetTokensAsync(string accessToken, string refreshToken);
    Task ClearAsync();
}