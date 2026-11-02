using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryManagement.Application.Dtos.Loans;
using LibraryManagement.Domain.Constants;
using LibraryManagement.WebApi.IntegrationTests.Fixtures;

namespace LibraryManagement.WebApi.IntegrationTests.Loans;

/// <summary>
/// POST /api/loans/{id}/return. Test plan IT-28 to IT-35.
/// </summary>
[Collection(ApiCollection.Name)]
public class ReturnTests : IntegrationTestBase
{
    public ReturnTests(LibraryApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Return_ActiveLoan_Returns200AndFreesCopy()
    {
        // Arrange
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync());
        var loanId = await Data.AddActiveLoanAsync(await Data.AddMemberAsync(), copyId, Today, Today.AddDays(14));

        // Act
        var response = await ReturnAsync(loanId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        (await Data.GetCopyStatusAsync(copyId)).Should().Be(CopyStatusIds.Available);
    }

    [Fact]
    public async Task Return_ActiveLoan_ReturnsLoanReturnedToday()
    {
        // Arrange
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync());
        var loanId = await Data.AddActiveLoanAsync(await Data.AddMemberAsync(), copyId, Today.AddDays(-3), Today.AddDays(11));

        // Act
        var response = await ReturnAsync(loanId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        var loan = await response.Content.ReadFromJsonAsync<LoanDto>();
        loan.Should().BeEquivalentTo(new { Id = loanId, Status = "Returned", ReturnDate = (DateOnly?)Today });
    }

    [Fact]
    public async Task Return_PastDueLoan_Returns200WithIsOverdueFalse()
    {
        // Arrange: lent today, then the clock moves 20 days, past the due date.
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync());
        var loanId = await Data.AddActiveLoanAsync(await Data.AddMemberAsync(), copyId, Today, Today.AddDays(14));
        Clock.Advance(TimeSpan.FromDays(20));

        // Act
        var response = await ReturnAsync(loanId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        var loan = await response.Content.ReadFromJsonAsync<LoanDto>();
        loan.Should().BeEquivalentTo(new { IsOverdue = false, ReturnDate = (DateOnly?)Today });
    }

    [Fact]
    public async Task Return_AlreadyReturnedLoan_Returns409LoanNotOpen()
    {
        // Arrange
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync());
        var loanId = await Data.AddReturnedLoanAsync(
            await Data.AddMemberAsync(), copyId, Today.AddDays(-10), Today.AddDays(4), Today.AddDays(-2));

        // Act
        var response = await ReturnAsync(loanId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Conflict);
        (await response.ReadProblemAsync()).Code.Should().Be("loan_not_open");
    }

    [Fact]
    public async Task Return_UnknownLoan_Returns404()
    {
        // Act
        var response = await ReturnAsync(999_999);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Return_InactiveMember_Returns200()
    {
        // Arrange
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync());
        var memberId = await Data.AddMemberAsync(isActive: false);
        var loanId = await Data.AddActiveLoanAsync(memberId, copyId, Today, Today.AddDays(14));

        // Act
        var response = await ReturnAsync(loanId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Return_SoftDeletedMember_Returns200()
    {
        // Arrange
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync());
        var memberId = await Data.AddMemberAsync(isDeleted: true);
        var loanId = await Data.AddActiveLoanAsync(memberId, copyId, Today, Today.AddDays(14));

        // Act
        var response = await ReturnAsync(loanId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Return_CopyOfSoftDeletedBook_Returns200()
    {
        // Arrange
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync(isDeleted: true));
        var loanId = await Data.AddActiveLoanAsync(await Data.AddMemberAsync(), copyId, Today, Today.AddDays(14));

        // Act
        var response = await ReturnAsync(loanId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
    }
}
