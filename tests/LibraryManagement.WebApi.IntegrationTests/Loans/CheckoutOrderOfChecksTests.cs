using System.Net;
using FluentAssertions;
using LibraryManagement.Domain.Constants;
using LibraryManagement.WebApi.IntegrationTests.Fixtures;

namespace LibraryManagement.WebApi.IntegrationTests.Loans;

/// <summary>
/// POST /api/loans when two rules fail at once: the check that comes first
/// in the spec's "Order of checks" decides the response. Test plan IT-21 to IT-27.
/// </summary>
[Collection(ApiCollection.Name)]
public class CheckoutOrderOfChecksTests : IntegrationTestBase
{
    private const long UnknownMemberId = 999_999;
    private const long UnknownCopyId = 999_998;

    public CheckoutOrderOfChecksTests(LibraryApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Checkout_UnknownMemberAndUnknownCopy_Returns404ForMember()
    {
        // Act
        var response = await CheckoutAsync(UnknownMemberId, UnknownCopyId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.NotFound);
        (await response.ReadProblemAsync()).Detail.Should().StartWith("Member ");
    }

    [Fact]
    public async Task Checkout_InactiveMemberAndUnknownCopy_Returns404ForCopy()
    {
        // Arrange
        var memberId = await Data.AddMemberAsync(isActive: false);

        // Act
        var response = await CheckoutAsync(memberId, UnknownCopyId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.NotFound);
        (await response.ReadProblemAsync()).Detail.Should().StartWith("BookCopy ");
    }

    [Fact]
    public async Task Checkout_InactiveMemberWithOverdueLoan_ReturnsMemberInactive()
    {
        // Arrange
        var memberId = await Data.AddMemberAsync(isActive: false);
        await AddOpenLoansAsync(memberId, count: 1, dueDate: Today.AddDays(-1));
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync());

        // Act
        var response = await CheckoutAsync(memberId, copyId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Conflict);
        (await response.ReadProblemAsync()).Code.Should().Be("member_inactive");
    }

    [Fact]
    public async Task Checkout_OverdueMemberAtLoanLimit_ReturnsMemberHasOverdueLoan()
    {
        // Arrange
        var memberId = await Data.AddMemberAsync();
        await AddOpenLoansAsync(memberId, count: 3, dueDate: Today.AddDays(-1));
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync());

        // Act
        var response = await CheckoutAsync(memberId, copyId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Conflict);
        (await response.ReadProblemAsync()).Code.Should().Be("member_has_overdue_loan");
    }

    [Fact]
    public async Task Checkout_MemberAtLimitAndCopyOfDeletedBook_ReturnsLoanLimitReached()
    {
        // Arrange
        var memberId = await Data.AddMemberAsync();
        await AddOpenLoansAsync(memberId, count: 3, dueDate: Today.AddDays(14));
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync(isDeleted: true));

        // Act
        var response = await CheckoutAsync(memberId, copyId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Conflict);
        (await response.ReadProblemAsync()).Code.Should().Be("loan_limit_reached");
    }

    [Fact]
    public async Task Checkout_UnavailableCopyOfDeletedBook_ReturnsBookDeleted()
    {
        // Arrange
        var memberId = await Data.AddMemberAsync();
        var copyId = await Data.AddCopyAsync(
            await Data.AddBookAsync(isDeleted: true),
            statusId: CopyStatusIds.Maintenance);

        // Act
        var response = await CheckoutAsync(memberId, copyId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Conflict);
        (await response.ReadProblemAsync()).Code.Should().Be("book_deleted");
    }

    [Fact]
    public async Task Checkout_UnavailableCopyOfBookAlreadyOnLoan_ReturnsCopyNotAvailable()
    {
        // Arrange: the member already holds one copy of the book, and asks for
        // another copy of it that someone else has borrowed.
        var bookId = await Data.AddBookAsync();
        var memberId = await Data.AddMemberAsync();
        await Data.AddActiveLoanAsync(memberId, await Data.AddCopyAsync(bookId), Today, Today.AddDays(14));
        var copyId = await Data.AddCopyAsync(bookId);
        await Data.AddActiveLoanAsync(await Data.AddMemberAsync(), copyId, Today, Today.AddDays(14));

        // Act
        var response = await CheckoutAsync(memberId, copyId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Conflict);
        (await response.ReadProblemAsync()).Code.Should().Be("copy_not_available");
    }

    private async Task AddOpenLoansAsync(long memberId, int count, DateOnly dueDate)
    {
        for (var i = 0; i < count; i++)
        {
            var copyId = await Data.AddCopyAsync(await Data.AddBookAsync());
            await Data.AddActiveLoanAsync(memberId, copyId, Today.AddDays(-20), dueDate);
        }
    }
}
