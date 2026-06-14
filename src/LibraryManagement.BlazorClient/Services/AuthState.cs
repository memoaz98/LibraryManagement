namespace LibraryManagement.BlazorClient.Services;

/// <summary>
/// In-memory authentication state for the current browser session.
/// Components can subscribe to <see cref="OnChange"/> to react when
/// the user logs in or out.
/// </summary>
public class AuthState
{
    public bool IsAuthenticated { get; private set; }
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }

    public event Action? OnChange;

    public void SetAuthenticated(string accessToken, string refreshToken)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        IsAuthenticated = true;
        NotifyStateChanged();
    }

    public void Clear()
    {
        AccessToken = null;
        RefreshToken = null;
        IsAuthenticated = false;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}