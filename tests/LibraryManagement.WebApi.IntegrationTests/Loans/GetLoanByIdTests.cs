using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryManagement.Application.Dtos.Loans;
using LibraryManagement.WebApi.IntegrationTests.Fixtures;

namespace LibraryManagement.WebApi.IntegrationTests.Loans;

/// <summary>
/// GET /api/loans/{id}. Test plan IT-38 to IT-45.
/// </summary>
[Collection(ApiCollection.Name)]
public class GetLoanByIdTests : IntegrationTestBase
{
    private const long UnknownLoanId = 999_999;

    public GetLoanByIdTests(LibraryApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetById_AsStaff_ReturnsAnyLoan()
    {
        // Arrange
        var loanId = await AddLoanAsync(await Data.AddMemberAsync());

        // Act
        var response = await CreateLibrarianClient().GetAsync($"/api/loans/{loanId}");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<LoanDto>())!.Id.Should().Be(loanId);
    }

    [Fact]
    public async Task GetById_AsReader_OwnLoan_Returns200()
    {
        // Arrange
        var userId = await Data.AddUserAsync();
        var loanId = await AddLoanAsync(await Data.AddMemberAsync(userId: userId));

        // Act
        var response = await CreateReaderClient(userId).GetAsync($"/api/loans/{loanId}");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<LoanDto>())!.Id.Should().Be(loanId);
    }

    [Fact]
    public async Task GetById_AsReader_OthersLoan_Returns404LikeMissingLoan()
    {
        // Arrange
        var userId = await Data.AddUserAsync();
        await Data.AddMemberAsync(userId: userId);
        var othersLoanId = await AddLoanAsync(await Data.AddMemberAsync());
        var reader = CreateReaderClient(userId);

        // Act
        var othersResponse = await reader.GetAsync($"/api/loans/{othersLoanId}");
        var missingResponse = await reader.GetAsync($"/api/loans/{UnknownLoanId}");

        // Assert: nothing tells the reader that the other loan exists.
        othersResponse.Should().HaveStatusCode(HttpStatusCode.NotFound);
        missingResponse.Should().HaveStatusCode(HttpStatusCode.NotFound);
        var others = await othersResponse.ReadProblemAsync();
        var missing = await missingResponse.ReadProblemAsync();
        others.Should().BeEquivalentTo(missing, options => options.Excluding(problem => problem.Detail));
        others.Detail.Should().Be(missing.Detail!.Replace($"{UnknownLoanId}", $"{othersLoanId}"));
    }

    [Fact]
    public async Task GetById_UnknownLoan_Returns404()
    {
        // Act
        var response = await CreateLibrarianClient().GetAsync($"/api/loans/{UnknownLoanId}");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_AsReaderWithoutLinkedMember_Returns404()
    {
        // Arrange
        var loanId = await AddLoanAsync(await Data.AddMemberAsync());

        // Act
        var response = await CreateReaderClient("reader-without-member").GetAsync($"/api/loans/{loanId}");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_OpenLoanPastDueDate_ReportsOverdue()
    {
        // Arrange
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync());
        var loanId = await Data.AddActiveLoanAsync(
            await Data.AddMemberAsync(), copyId, Today.AddDays(-20), dueDate: Today.AddDays(-1));

        // Act
        var response = await CreateLibrarianClient().GetAsync($"/api/loans/{loanId}");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        var loan = await response.Content.ReadFromJsonAsync<LoanDto>();
        loan.Should().BeEquivalentTo(new { Status = "Active", IsOverdue = true });
    }

    [Fact]
    public async Task GetById_OpenLoanDueToday_ReportsNotOverdue()
    {
        // Arrange
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync());
        var loanId = await Data.AddActiveLoanAsync(
            await Data.AddMemberAsync(), copyId, Today.AddDays(-14), dueDate: Today);

        // Act
        var response = await CreateLibrarianClient().GetAsync($"/api/loans/{loanId}");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<LoanDto>())!.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public async Task GetById_SoftDeletedMemberAndBook_StillReturnsDisplayFields()
    {
        // Arrange: member, book and copy were all soft-deleted after the loan closed.
        var bookId = await Data.AddBookAsync(title: "Old Title", isDeleted: true);
        var copyId = await Data.AddCopyAsync(bookId, inventoryCode: "INV-0009", isDeleted: true);
        var memberId = await Data.AddMemberAsync(firstName: "Ana", lastName: "Mora", isDeleted: true);
        var loanId = await Data.AddReturnedLoanAsync(memberId, copyId, Today.AddDays(-30), Today.AddDays(-16), Today.AddDays(-20));

        // Act
        var response = await CreateLibrarianClient().GetAsync($"/api/loans/{loanId}");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        var loan = await response.Content.ReadFromJsonAsync<LoanDto>();
        loan.Should().BeEquivalentTo(new { MemberName = "Ana Mora", InventoryCode = "INV-0009", BookTitle = "Old Title" });
    }

    private async Task<long> AddLoanAsync(long memberId)
    {
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync());
        return await Data.AddActiveLoanAsync(memberId, copyId, Today, Today.AddDays(14));
    }
}
