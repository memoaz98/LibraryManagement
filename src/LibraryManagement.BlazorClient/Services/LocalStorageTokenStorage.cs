using Microsoft.JSInterop;

namespace LibraryManagement.BlazorClient.Services;

/// <summary>
/// Persists tokens in the browser's localStorage via JS interop.
///
/// Trade-off: localStorage is vulnerable to XSS. If an attacker injects
/// JavaScript into the page, they can read these tokens. The mitigation
/// is short-lived access tokens (15 min) and refresh token rotation,
/// limiting the blast radius of a leak.
///
/// For maximum security in enterprise scenarios, use HttpOnly cookies
/// set by the server instead.
/// </summary>
public class LocalStorageTokenStorage : ITokenStorage
{
    private const string AccessTokenKey = "library.accessToken";
    private const string RefreshTokenKey = "library.refreshToken";

    private readonly IJSRuntime _js;

    public LocalStorageTokenStorage(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        return await _js.InvokeAsync<string?>("localStorage.getItem", AccessTokenKey);
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        return await _js.InvokeAsync<string?>("localStorage.getItem", RefreshTokenKey);
    }

    public async Task SetTokensAsync(string accessToken, string refreshToken)
    {
        await _js.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, accessToken);
        await _js.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, refreshToken);
    }

    public async Task ClearAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
        await _js.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
    }
}