using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryManagement.Application.Dtos.Loans;
using LibraryManagement.WebApi.IntegrationTests.Fixtures;

namespace LibraryManagement.WebApi.IntegrationTests.Loans;

/// <summary>
/// GET /api/loans for staff. Test plan IT-46 to IT-55.
/// </summary>
[Collection(ApiCollection.Name)]
public class ListLoansTests : IntegrationTestBase
{
    public ListLoansTests(LibraryApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task List_NoFilters_ReturnsAllSortedByLoanDateDescThenIdDesc()
    {
        // Arrange
        var memberA = await Data.AddMemberAsync();
        var memberB = await Data.AddMemberAsync();
        var oldest = await Data.AddReturnedLoanAsync(memberA, await NewCopyAsync(), Today.AddDays(-30), Today.AddDays(-16), Today.AddDays(-20));
        var sameDayFirst = await Data.AddActiveLoanAsync(memberB, await NewCopyAsync(), Today.AddDays(-2), Today.AddDays(12));
        var sameDaySecond = await Data.AddActiveLoanAsync(memberA, await NewCopyAsync(), Today.AddDays(-2), Today.AddDays(12));
        var middle = await Data.AddActiveLoanAsync(memberB, await NewCopyAsync(), Today.AddDays(-5), Today.AddDays(9));

        // Act
        var response = await CreateLibrarianClient().GetAsync("/api/loans");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        (await ReadIdsAsync(response)).Should().Equal(sameDaySecond, sameDayFirst, middle, oldest);
    }

    [Fact]
    public async Task List_ByMember_ReturnsOnlyThatMembersLoans()
    {
        // Arrange
        var memberA = await Data.AddMemberAsync();
        var memberB = await Data.AddMemberAsync();
        var first = await Data.AddActiveLoanAsync(memberA, await NewCopyAsync(), Today, Today.AddDays(14));
        var second = await Data.AddReturnedLoanAsync(memberA, await NewCopyAsync(), Today.AddDays(-30), Today.AddDays(-16), Today.AddDays(-20));
        await Data.AddActiveLoanAsync(memberB, await NewCopyAsync(), Today, Today.AddDays(14));

        // Act
        var response = await CreateLibrarianClient().GetAsync($"/api/loans?memberId={memberA}");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        (await ReadIdsAsync(response)).Should().BeEquivalentTo(new[] { first, second });
    }

    [Fact]
    public async Task List_ByCopy_ReturnsOnlyThatCopysLoans()
    {
        // Arrange
        var copyX = await NewCopyAsync();
        var earlier = await Data.AddReturnedLoanAsync(await Data.AddMemberAsync(), copyX, Today.AddDays(-30), Today.AddDays(-16), Today.AddDays(-20));
        var current = await Data.AddActiveLoanAsync(await Data.AddMemberAsync(), copyX, Today, Today.AddDays(14));
        await Data.AddActiveLoanAsync(await Data.AddMemberAsync(), await NewCopyAsync(), Today, Today.AddDays(14));

        // Act
        var response = await CreateLibrarianClient().GetAsync($"/api/loans?bookCopyId={copyX}");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        (await ReadIdsAsync(response)).Should().BeEquivalentTo(new[] { earlier, current });
    }

    [Fact]
    public async Task List_ByMemberAndCopy_ReturnsOnlyLoansMatchingBoth()
    {
        // Arrange
        var memberA = await Data.AddMemberAsync();
        var memberB = await Data.AddMemberAsync();
        var copyX = await NewCopyAsync();
        var match = await Data.AddReturnedLoanAsync(memberA, copyX, Today.AddDays(-30), Today.AddDays(-16), Today.AddDays(-20));
        await Data.AddActiveLoanAsync(memberB, copyX, Today, Today.AddDays(14));
        await Data.AddActiveLoanAsync(memberA, await NewCopyAsync(), Today, Today.AddDays(14));

        // Act
        var response = await CreateLibrarianClient().GetAsync($"/api/loans?memberId={memberA}&bookCopyId={copyX}");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        (await ReadIdsAsync(response)).Should().Equal(match);
    }

    [Fact]
    public async Task List_StatusActive_ReturnsActiveAndOverdueLoans()
    {
        // Arrange
        var memberId = await Data.AddMemberAsync();
        var active = await Data.AddActiveLoanAsync(memberId, await NewCopyAsync(), Today, Today.AddDays(14));
        var overdue = await Data.AddOverdueLoanAsync(memberId, await NewCopyAsync(), Today.AddDays(-20), Today.AddDays(-6));
        await Data.AddReturnedLoanAsync(memberId, await NewCopyAsync(), Today.AddDays(-30), Today.AddDays(-16), Today.AddDays(-20));

        // Act
        var response = await CreateLibrarianClient().GetAsync("/api/loans?status=active");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        (await ReadIdsAsync(response)).Should().BeEquivalentTo(new[] { active, overdue });
    }

    [Fact]
    public async Task List_StatusReturned_ReturnsOnlyReturnedLoans()
    {
        // Arrange
        var memberId = await Data.AddMemberAsync();
        await Data.AddActiveLoanAsync(memberId, await NewCopyAsync(), Today, Today.AddDays(14));
        var returned = await Data.AddReturnedLoanAsync(memberId, await NewCopyAsync(), Today.AddDays(-30), Today.AddDays(-16), Today.AddDays(-20));

        // Act
        var response = await CreateLibrarianClient().GetAsync("/api/loans?status=returned");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        (await ReadIdsAsync(response)).Should().Equal(returned);
    }

    [Theory]
    [InlineData("Active")]
    [InlineData("ACTIVE")]
    public async Task List_StatusFilter_IsCaseInsensitive(string status)
    {
        // Arrange
        var memberId = await Data.AddMemberAsync();
        var active = await Data.AddActiveLoanAsync(memberId, await NewCopyAsync(), Today, Today.AddDays(14));
        await Data.AddReturnedLoanAsync(memberId, await NewCopyAsync(), Today.AddDays(-30), Today.AddDays(-16), Today.AddDays(-20));

        // Act
        var response = await CreateLibrarianClient().GetAsync($"/api/loans?status={status}");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        (await ReadIdsAsync(response)).Should().Equal(active);
    }

    [Theory]
    [InlineData("overdue")]
    [InlineData("open")]
    [InlineData("x")]
    public async Task List_InvalidStatus_Returns400(string status)
    {
        // Act
        var response = await CreateLibrarianClient().GetAsync($"/api/loans?status={status}");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("memberId=0")]
    [InlineData("memberId=-1")]
    [InlineData("bookCopyId=0")]
    public async Task List_NonPositiveIdFilter_Returns400(string query)
    {
        // Act
        var response = await CreateLibrarianClient().GetAsync($"/api/loans?{query}");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_UnknownMember_ReturnsEmptyList()
    {
        // Arrange
        await Data.AddActiveLoanAsync(await Data.AddMemberAsync(), await NewCopyAsync(), Today, Today.AddDays(14));

        // Act
        var response = await CreateLibrarianClient().GetAsync("/api/loans?memberId=999999");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        (await ReadIdsAsync(response)).Should().BeEmpty();
    }

    private async Task<long> NewCopyAsync()
    {
        return await Data.AddCopyAsync(await Data.AddBookAsync());
    }

    private static async Task<long[]> ReadIdsAsync(HttpResponseMessage response)
    {
        var loans = await response.Content.ReadFromJsonAsync<List<LoanDto>>();
        return loans!.Select(loan => loan.Id).ToArray();
    }
}
