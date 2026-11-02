using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryManagement.Application.Dtos.Loans;
using LibraryManagement.Domain.Constants;
using LibraryManagement.WebApi.IntegrationTests.Fixtures;

namespace LibraryManagement.WebApi.IntegrationTests.Loans;

/// <summary>
/// GET /api/members/me/loans. Test plan IT-56 to IT-62.
/// </summary>
[Collection(ApiCollection.Name)]
public class MyLoansTests : IntegrationTestBase
{
    public MyLoansTests(LibraryApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task MyLoans_LinkedReader_ReturnsOnlyOwn()
    {
        // Arrange: two members, only one linked to the reader's account.
        var userId = await Data.AddUserAsync();
        var ownMemberId = await Data.AddMemberAsync(userId: userId);
        var otherMemberId = await Data.AddMemberAsync();
        var first = await Data.AddActiveLoanAsync(ownMemberId, await NewCopyAsync(), Today, Today.AddDays(14));
        var second = await Data.AddReturnedLoanAsync(ownMemberId, await NewCopyAsync(), Today.AddDays(-30), Today.AddDays(-16), Today.AddDays(-20));
        await Data.AddActiveLoanAsync(otherMemberId, await NewCopyAsync(), Today, Today.AddDays(14));

        // Act
        var response = await CreateReaderClient(userId).GetAsync("/api/members/me/loans");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        (await ReadIdsAsync(response)).Should().BeEquivalentTo(new[] { first, second });
    }

    [Fact]
    public async Task MyLoans_UnlinkedReader_Returns200Empty()
    {
        // Arrange
        await Data.AddActiveLoanAsync(await Data.AddMemberAsync(), await NewCopyAsync(), Today, Today.AddDays(14));

        // Act
        var response = await CreateReaderClient("reader-without-member").GetAsync("/api/members/me/loans");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        (await ReadIdsAsync(response)).Should().BeEmpty();
    }

    [Fact]
    public async Task MyLoans_StatusActive_ReturnsOnlyOpenOwnLoans()
    {
        // Arrange
        var userId = await Data.AddUserAsync();
        var memberId = await Data.AddMemberAsync(userId: userId);
        var active = await Data.AddActiveLoanAsync(memberId, await NewCopyAsync(), Today, Today.AddDays(14));
        await Data.AddReturnedLoanAsync(memberId, await NewCopyAsync(), Today.AddDays(-30), Today.AddDays(-16), Today.AddDays(-20));

        // Act
        var response = await CreateReaderClient(userId).GetAsync("/api/members/me/loans?status=active");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        (await ReadIdsAsync(response)).Should().Equal(active);
    }

    [Fact]
    public async Task MyLoans_InvalidStatus_Returns400()
    {
        // Arrange
        var userId = await Data.AddUserAsync();
        await Data.AddMemberAsync(userId: userId);

        // Act
        var response = await CreateReaderClient(userId).GetAsync("/api/members/me/loans?status=x");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MyLoans_DeactivatedMember_StillReturnsOwnLoans()
    {
        // Arrange
        var userId = await Data.AddUserAsync();
        var memberId = await Data.AddMemberAsync(userId: userId, isActive: false);
        var loanId = await Data.AddActiveLoanAsync(memberId, await NewCopyAsync(), Today, Today.AddDays(14));

        // Act
        var response = await CreateReaderClient(userId).GetAsync("/api/members/me/loans");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        (await ReadIdsAsync(response)).Should().Equal(loanId);
    }

    [Fact]
    public async Task MyLoans_SoftDeletedMember_ReturnsEmpty()
    {
        // Arrange
        var userId = await Data.AddUserAsync();
        var memberId = await Data.AddMemberAsync(userId: userId, isDeleted: true);
        await Data.AddActiveLoanAsync(memberId, await NewCopyAsync(), Today, Today.AddDays(14));

        // Act
        var response = await CreateReaderClient(userId).GetAsync("/api/members/me/loans");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        (await ReadIdsAsync(response)).Should().BeEmpty();
    }

    [Fact]
    public async Task MyLoans_StaffLinkedToMember_ReturnsOwnLoans()
    {
        // Arrange
        var userId = await Data.AddUserAsync();
        var memberId = await Data.AddMemberAsync(userId: userId);
        var loanId = await Data.AddActiveLoanAsync(memberId, await NewCopyAsync(), Today, Today.AddDays(14));
        await Data.AddActiveLoanAsync(await Data.AddMemberAsync(), await NewCopyAsync(), Today, Today.AddDays(14));
        var librarian = Factory.CreateClientFor(userId, RoleIds.Names.Librarian);

        // Act
        var response = await librarian.GetAsync("/api/members/me/loans");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        (await ReadIdsAsync(response)).Should().Equal(loanId);
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
