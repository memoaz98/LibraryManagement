using System.Net.Http.Headers;
using System.Security.Cryptography;
using LibraryManagement.Application.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Respawn;
using Testcontainers.MsSql;

namespace LibraryManagement.WebApi.IntegrationTests.Fixtures;

/// <summary>
/// Hosts the real API in memory against a throwaway SQL Server container.
/// One instance is shared by every test in <see cref="ApiCollection"/>.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>The schema comes from the Flyway scripts in <c>database/migrations</c>.</item>
///   <item><see cref="Clock"/> replaces the app's <see cref="TimeProvider"/>, so tests control "today".</item>
///   <item><see cref="ResetAsync"/> deletes test data (the V002 catalog rows stay) and resets the clock.</item>
/// </list>
/// </remarks>
public class LibraryApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>
    /// The clock's starting point before every test. Fixed, so expected dates
    /// are deterministic.
    /// </summary>
    public static readonly DateTimeOffset DefaultNow = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    private const string DatabaseName = "LibraryDb";

    // Same image as database/docker-compose.yml, so it is usually cached.
    private readonly MsSqlContainer _sqlContainer =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    private Respawner _respawner = default!;

    public TestClock Clock { get; } = new(DefaultNow);

    public string ConnectionString { get; private set; } = default!;

    /// <summary>Generated per run; never a real key.</summary>
    private string JwtSigningKey { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();

        // The container's connection string targets master; the scripts need their own database.
        var masterConnectionString = _sqlContainer.GetConnectionString();
        await DatabaseMigrator.CreateDatabaseAsync(masterConnectionString, DatabaseName);

        ConnectionString = new SqlConnectionStringBuilder(masterConnectionString)
        {
            InitialCatalog = DatabaseName
        }.ConnectionString;

        await DatabaseMigrator.ApplyMigrationsAsync(ConnectionString);

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            // Seeded by V002; the code relies on their fixed ids.
            TablesToIgnore = ["CopyStatus", "LoanStatus", "AspNetRoles"]
        });
    }

    /// <summary>
    /// Empties every table except the catalogs, and puts the clock back to
    /// <see cref="DefaultNow"/>.
    /// </summary>
    public async Task ResetAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);

        Clock.SetUtcNow(DefaultNow);
    }

    /// <summary>
    /// A client whose requests carry an access token for <paramref name="userId"/>
    /// with the given roles.
    /// </summary>
    public HttpClient CreateClientFor(string userId, params string[] roles)
    {
        var jwtOptions = Services.GetRequiredService<IOptions<JwtOptions>>().Value;
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokens.Create(jwtOptions, userId, roles));
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Program.cs reads these before Build(), so they go in as host settings.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:LibraryDb", ConnectionString);
        builder.UseSetting("Jwt:SigningKey", JwtSigningKey);
        builder.UseSetting("DataAccess:Provider", "EntityFramework");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _sqlContainer.DisposeAsync();
    }
}
