using System.Text.Json;

namespace LibraryManagement.WebApi.IntegrationTests.Fixtures;

/// <summary>
/// The parts of a ProblemDetails body the tests assert on. <see cref="Code"/>
/// is the reason-code extension on 409 responses (spec D-19).
/// </summary>
public sealed record ProblemResponse(int Status, string? Title, string? Detail, string? Code);

public static class ProblemResponseExtensions
{
    public static async Task<ProblemResponse> ReadProblemAsync(this HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        return new ProblemResponse(
            Status: root.TryGetProperty("status", out var status) ? status.GetInt32() : 0,
            Title: GetString(root, "title"),
            Detail: GetString(root, "detail"),
            Code: GetString(root, "code"));
    }

    private static string? GetString(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}
