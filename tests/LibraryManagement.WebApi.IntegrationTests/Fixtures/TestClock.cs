namespace LibraryManagement.WebApi.IntegrationTests.Fixtures;

/// <summary>
/// The API's clock during integration tests. It can be set to any instant,
/// backwards included: <c>FakeTimeProvider</c> refuses to go back in time, but
/// the shared host must put the clock back to
/// <see cref="LibraryApiFactory.DefaultNow"/> before every test.
/// </summary>
public sealed class TestClock : TimeProvider
{
    private readonly object _gate = new();
    private DateTimeOffset _utcNow;

    public TestClock(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow()
    {
        lock (_gate)
        {
            return _utcNow;
        }
    }

    /// <summary>Moves the clock to <paramref name="utcNow"/>, forwards or backwards.</summary>
    public void SetUtcNow(DateTimeOffset utcNow)
    {
        lock (_gate)
        {
            _utcNow = utcNow;
        }
    }

    public void Advance(TimeSpan delta)
    {
        lock (_gate)
        {
            _utcNow += delta;
        }
    }
}
