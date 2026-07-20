using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LibraryManagement.BlazorClient.Models.Auth;

namespace LibraryManagement.BlazorClient.Services;

/// <summary>
/// HTTP message handler that:
/// 1. Injects the Bearer access token into every outgoing request.
/// 2. Intercepts 401 responses, attempts a token refresh, and retries
///    the original request transparently.
///
/// Registered as a typed HttpClient delegating handler in Program.cs.
/// </summary>
public class AuthMessageHandler : DelegatingHandler
{
    private readonly ITokenStorage _tokenStorage;
    private readonly AuthState _authState;
    private readonly IServiceProvider _serviceProvider;

    public AuthMessageHandler(
        ITokenStorage tokenStorage,
        AuthState authState,
        IServiceProvider serviceProvider)
    {
        _tokenStorage = tokenStorage;
        _authState = authState;
        _serviceProvider = serviceProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (IsAuthEndpoint(request))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        var accessToken = await _tokenStorage.GetAccessTokenAsync();
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        var refreshed = await TryRefreshTokenAsync(cancellationToken);
        if (!refreshed)
        {
            await _tokenStorage.ClearAsync();
            _authState.Clear();
            return response;
        }

        var newAccessToken = await _tokenStorage.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(newAccessToken))
        {
            return response;
        }

        var retryRequest = await CloneRequestAsync(request);
        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);

        response.Dispose();
        return await base.SendAsync(retryRequest, cancellationToken);
    }

    private static bool IsAuthEndpoint(HttpRequestMessage request)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        return path.StartsWith("/api/auth/", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> TryRefreshTokenAsync(CancellationToken cancellationToken)
    {
        var refreshToken = await _tokenStorage.GetRefreshTokenAsync();
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return false;
        }

        using var scope = _serviceProvider.CreateScope();
        var http = scope.ServiceProvider.GetRequiredService<HttpClient>();

        try
        {
            var response = await http.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest { RefreshToken = refreshToken },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken);
            if (auth is null)
            {
                return false;
            }

            await _tokenStorage.SetTokensAsync(auth.AccessToken, auth.RefreshToken);
            _authState.SetAuthenticated(auth.AccessToken, auth.RefreshToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);

        foreach (var header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (original.Content is not null)
        {
            var contentBytes = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(contentBytes);

            foreach (var header in original.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }
}