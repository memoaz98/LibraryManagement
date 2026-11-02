using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryManagement.Application.Dtos.Members;
using LibraryManagement.WebApi.IntegrationTests.Fixtures;

namespace LibraryManagement.WebApi.IntegrationTests.Lookups;

/// <summary>
/// GET /api/members?search=. Test plan IT-63 to IT-66.
/// </summary>
[Collection(ApiCollection.Name)]
public class MemberSearchTests : IntegrationTestBase
{
    public MemberSearchTests(LibraryApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Search_NoTerm_ReturnsAllIncludingInactive_SortedByLastThenFirstName()
    {
        // Arrange
        var luis = await Data.AddMemberAsync(firstName: "Luis", lastName: "Zamora");
        var bea = await Data.AddMemberAsync(firstName: "Bea", lastName: "Mora");
        var ana = await Data.AddMemberAsync(firstName: "Ana", lastName: "Mora", isActive: false);

        // Act
        var response = await CreateLibrarianClient().GetAsync("/api/members");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        var members = (await response.Content.ReadFromJsonAsync<List<MemberDto>>())!;
        members.Select(member => member.Id).Should().Equal(ana, bea, luis);
        members.Single(member => member.Id == ana).IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData("aN")]
    [InlineData("quES")]
    [InlineData("READER.ONE")]
    public async Task Search_MatchesFirstLastOrEmail_CaseInsensitiveContains(string term)
    {
        // Arrange: "aN" only matches Ana's first name, "quES" her last name,
        // and "READER.ONE" her email.
        var ana = await Data.AddMemberAsync(firstName: "Ana", lastName: "Quesada", email: "reader.one@example.org");
        await Data.AddMemberAsync(firstName: "Luis", lastName: "Zamora", email: "luis@other.test");

        // Act
        var response = await CreateLibrarianClient().GetAsync($"/api/members?search={term}");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        (await ReadIdsAsync(response)).Should().Equal(ana);
    }

    [Fact]
    public async Task Search_NoMatch_ReturnsEmptyList()
    {
        // Arrange
        await Data.AddMemberAsync(firstName: "Ana", lastName: "Quesada");

        // Act
        var response = await CreateLibrarianClient().GetAsync("/api/members?search=nobody");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        (await ReadIdsAsync(response)).Should().BeEmpty();
    }

    [Fact]
    public async Task Search_ExcludesSoftDeletedMembers()
    {
        // Arrange
        var active = await Data.AddMemberAsync();
        await Data.AddMemberAsync(isDeleted: true);

        // Act
        var response = await CreateLibrarianClient().GetAsync("/api/members");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        (await ReadIdsAsync(response)).Should().Equal(active);
    }

    private static async Task<long[]> ReadIdsAsync(HttpResponseMessage response)
    {
        var members = await response.Content.ReadFromJsonAsync<List<MemberDto>>();
        return members!.Select(member => member.Id).ToArray();
    }
}
