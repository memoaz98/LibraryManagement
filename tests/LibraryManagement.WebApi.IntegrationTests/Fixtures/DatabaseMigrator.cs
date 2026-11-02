using Microsoft.Data.SqlClient;

namespace LibraryManagement.WebApi.IntegrationTests.Fixtures;

/// <summary>
/// Builds the test database from the Flyway scripts copied into the
/// <c>Migrations</c> output folder (see the .csproj).
/// </summary>
internal static class DatabaseMigrator
{
    public static async Task CreateDatabaseAsync(string masterConnectionString, string databaseName)
    {
        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand($"CREATE DATABASE [{databaseName}];", connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Applies the versioned scripts (<c>V001__...</c>, <c>V002__...</c>) in
    /// version order. The scripts have no <c>GO</c> separators, so each one
    /// runs as a single batch.
    /// </summary>
    public static async Task ApplyMigrationsAsync(string connectionString)
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "Migrations");
        var scripts = Directory.GetFiles(folder, "V*__*.sql")
            .OrderBy(ParseVersion)
            .ToList();

        if (scripts.Count == 0)
        {
            throw new InvalidOperationException($"No migration scripts found in '{folder}'.");
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        foreach (var script in scripts)
        {
            var sql = await File.ReadAllTextAsync(script);
            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 120 };
            await command.ExecuteNonQueryAsync();
        }
    }

    private static int ParseVersion(string path)
    {
        // "V001__initial_schema.sql" -> 1
        var name = Path.GetFileName(path);
        return int.Parse(name[1..name.IndexOf("__", StringComparison.Ordinal)]);
    }
}
