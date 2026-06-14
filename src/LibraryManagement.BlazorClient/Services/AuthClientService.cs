using System.Net;
using System.Net.Http.Json;
using LibraryManagement.BlazorClient.Models.Auth;

namespace LibraryManagement.BlazorClient.Services;

/// <summary>
/// Client-side wrapper around the backend's /api/auth/* endpoints.
/// Persists tokens via <see cref="ITokenStorage"/> and updates the
/// shared <see cref="AuthState"/> so the UI can react.
/// </summary>
public class AuthClientService
{
    private readonly HttpClient _http;
    private readonly ITokenStorage _tokenStorage;
    private readonly AuthState _authState;

    public AuthClientService(
        HttpClient http,
        ITokenStorage tokenStorage,
        AuthState authState)
    {
        _http = http;
        _tokenStorage = tokenStorage;
        _authState = authState;
    }

    /// <summary>
    /// Restores authentication from persistent storage. Call this once at
    /// app startup so a returning user stays logged in.
    /// </summary>
    public async Task RestoreFromStorageAsync()
    {
        var accessToken = await _tokenStorage.GetAccessTokenAsync();
        var refreshToken = await _tokenStorage.GetRefreshTokenAsync();

        if (!string.IsNullOrWhiteSpace(accessToken) && !string.IsNullOrWhiteSpace(refreshToken))
        {
            _authState.SetAuthenticated(accessToken, refreshToken);
        }
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = password });

        return await HandleAuthResponseAsync(response);
    }

    public async Task<AuthResult> RegisterAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest { Email = email, Password = password });

        return await HandleAuthResponseAsync(response);
    }

    public async Task LogoutAsync()
    {
        var refreshToken = await _tokenStorage.GetRefreshTokenAsync();
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            try
            {
                await _http.PostAsJsonAsync("/api/auth/logout",
                    new RefreshTokenRequest { RefreshToken = refreshToken });
            }
            catch
            {
                // best-effort: even if the server call fails, clear locally.
            }
        }

        await _tokenStorage.ClearAsync();
        _authState.Clear();
    }

    private async Task<AuthResult> HandleAuthResponseAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (auth is null)
            {
                return AuthResult.Failure("Server returned an empty response.");
            }

            await _tokenStorage.SetTokensAsync(auth.AccessToken, auth.RefreshToken);
            _authState.SetAuthenticated(auth.AccessToken, auth.RefreshToken);

            return AuthResult.Success();
        }

        var errorMessage = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Invalid email or password.",
            HttpStatusCode.Conflict => "An account with that email already exists.",
            HttpStatusCode.BadRequest => await ExtractValidationErrorAsync(response),
            _ => $"Unexpected error: {(int)response.StatusCode} {response.StatusCode}."
        };

        return AuthResult.Failure(errorMessage);
    }

    private static async Task<string> ExtractValidationErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(content) ? "Invalid input." : content;
        }
        catch
        {
            return "Invalid input.";
        }
    }
}

/// <summary>
/// Result of an auth operation (login / register). Avoids throwing
/// exceptions for expected failure cases (bad credentials, validation),
/// while still letting callers branch cleanly.
/// </summary>
public record AuthResult(bool IsSuccess, string? ErrorMessage)
{
    public static AuthResult Success() => new(true, null);
    public static AuthResult Failure(string message) => new(false, message);
}