namespace LibraryManagement.WebApi.IntegrationTests.Fixtures;

/// <summary>
/// Base class for integration tests. Each test starts from an empty database
/// (catalog rows kept) and the clock at <see cref="LibraryApiFactory.DefaultNow"/>.
/// </summary>
/// <remarks>
/// Derived classes must also be marked <c>[Collection(ApiCollection.Name)]</c>.
/// </remarks>
[Collection(ApiCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected IntegrationTestBase(LibraryApiFactory factory)
    {
        Factory = factory;
    }

    protected LibraryApiFactory Factory { get; }

    protected TestClock Clock => Factory.Clock;

    public Task InitializeAsync()
    {
        return Factory.ResetAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
