using System.Net.Http.Json;
using LibraryManagement.Domain.Constants;

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
        Data = new TestData(factory.ConnectionString);
    }

    protected LibraryApiFactory Factory { get; }

    protected TestClock Clock => Factory.Clock;

    protected TestData Data { get; }

    /// <summary>Today's UTC date according to <see cref="Clock"/>.</summary>
    protected DateOnly Today => DateOnly.FromDateTime(Clock.GetUtcNow().UtcDateTime);

    public Task InitializeAsync()
    {
        return Factory.ResetAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    protected HttpClient CreateLibrarianClient()
    {
        return Factory.CreateClientFor("test-librarian", RoleIds.Names.Librarian);
    }

    protected HttpClient CreateReaderClient(string userId)
    {
        return Factory.CreateClientFor(userId, RoleIds.Names.Reader);
    }

    /// <summary>POST /api/loans as a Librarian, unless another client is given.</summary>
    protected Task<HttpResponseMessage> CheckoutAsync(long memberId, long bookCopyId, HttpClient? client = null)
    {
        return (client ?? CreateLibrarianClient()).PostAsJsonAsync(
            "/api/loans",
            new { memberId, bookCopyId });
    }

    /// <summary>POST /api/loans/{id}/return as a Librarian.</summary>
    protected Task<HttpResponseMessage> ReturnAsync(long loanId)
    {
        return CreateLibrarianClient().PostAsync($"/api/loans/{loanId}/return", content: null);
    }
}
