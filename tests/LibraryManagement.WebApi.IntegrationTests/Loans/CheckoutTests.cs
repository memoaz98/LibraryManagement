using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using LibraryManagement.Application.Dtos.Loans;
using LibraryManagement.Domain.Constants;
using LibraryManagement.WebApi.IntegrationTests.Fixtures;

namespace LibraryManagement.WebApi.IntegrationTests.Loans;

/// <summary>
/// POST /api/loans. Test plan IT-01 to IT-20 in docs/features/loan-checkout-return.md.
/// </summary>
[Collection(ApiCollection.Name)]
public class CheckoutTests : IntegrationTestBase
{
    private const long UnknownId = 999_999;

    public CheckoutTests(LibraryApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Checkout_Valid_Returns201WithLocationAndBody()
    {
        // Arrange
        var bookId = await Data.AddBookAsync(title: "Clean Code");
        var copyId = await Data.AddCopyAsync(bookId, inventoryCode: "INV-0001");
        var memberId = await Data.AddMemberAsync(firstName: "Ana", lastName: "Mora");

        // Act
        var response = await CheckoutAsync(memberId, copyId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Created);
        var loan = await response.Content.ReadFromJsonAsync<LoanDto>();
        loan!.Id.Should().BePositive();
        response.Headers.Location!.AbsolutePath.Should().Be($"/api/loans/{loan.Id}");
        loan.Should().BeEquivalentTo(new
        {
            MemberId = memberId,
            MemberName = "Ana Mora",
            BookCopyId = copyId,
            InventoryCode = "INV-0001",
            BookId = bookId,
            BookTitle = "Clean Code",
            Status = "Active",
            LoanDate = Today,
            DueDate = Today.AddDays(14),
            ReturnDate = (DateOnly?)null,
            IsOverdue = false
        });
    }

    [Fact]
    public async Task Checkout_Valid_PersistsCopyAsBorrowed()
    {
        // Arrange
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync());
        var memberId = await Data.AddMemberAsync();

        // Act
        var response = await CheckoutAsync(memberId, copyId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Created);
        (await Data.GetCopyStatusAsync(copyId)).Should().Be(CopyStatusIds.Borrowed);
    }

    [Fact]
    public async Task Checkout_Valid_PersistsOneActiveLoan()
    {
        // Arrange
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync());
        var memberId = await Data.AddMemberAsync();

        // Act
        var response = await CheckoutAsync(memberId, copyId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Created);
        (await Data.CountOpenLoansForCopyAsync(copyId)).Should().Be(1);
    }

    [Fact]
    public async Task Checkout_CopyAlreadyOnOpenLoan_Returns409AndKeepsOneLoan()
    {
        // Arrange: another member holds an open loan but the copy row still
        // says Available, as if this request had lost a race. Only the unique
        // index on open loans can refuse it.
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync());
        var holderId = await Data.AddMemberAsync();
        await Data.AddActiveLoanAsync(holderId, copyId, Today, Today.AddDays(14), borrowCopy: false);
        var memberId = await Data.AddMemberAsync();

        // Act
        var response = await CheckoutAsync(memberId, copyId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Conflict);
        (await response.ReadProblemAsync()).Code.Should().Be("copy_not_available");
        (await Data.CountOpenLoansForCopyAsync(copyId)).Should().Be(1);
    }

    [Fact]
    public async Task Checkout_ConcurrentRequestsForSameCopy_ExactlyOneSucceeds()
    {
        // Arrange
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync());
        var firstMemberId = await Data.AddMemberAsync();
        var secondMemberId = await Data.AddMemberAsync();

        // Act
        var responses = await Task.WhenAll(
            CheckoutAsync(firstMemberId, copyId),
            CheckoutAsync(secondMemberId, copyId));

        // Assert
        responses.Select(response => response.StatusCode).Should().BeEquivalentTo(
            new[] { HttpStatusCode.Created, HttpStatusCode.Conflict });
    }

    [Fact]
    public async Task Checkout_InactiveMember_Returns409MemberInactive()
    {
        // Arrange
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync());
        var memberId = await Data.AddMemberAsync(isActive: false);

        // Act
        var response = await CheckoutAsync(memberId, copyId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Conflict);
        (await response.ReadProblemAsync()).Code.Should().Be("member_inactive");
    }

    [Fact]
    public async Task Checkout_MemberWithOverdueLoan_Returns409MemberHasOverdueLoan()
    {
        // Arrange
        var memberId = await Data.AddMemberAsync();
        var heldCopyId = await Data.AddCopyAsync(await Data.AddBookAsync());
        await Data.AddActiveLoanAsync(memberId, heldCopyId, Today.AddDays(-20), dueDate: Today.AddDays(-1));
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync());

        // Act
        var response = await CheckoutAsync(memberId, copyId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Conflict);
        (await response.ReadProblemAsync()).Code.Should().Be("member_has_overdue_loan");
    }

    [Fact]
    public async Task Checkout_MemberAtLoanLimit_Returns409LoanLimitReached()
    {
        // Arrange
        var memberId = await Data.AddMemberAsync();
        for (var i = 0; i < 3; i++)
        {
            var heldCopyId = await Data.AddCopyAsync(await Data.AddBookAsync());
            await Data.AddActiveLoanAsync(memberId, heldCopyId, Today, Today.AddDays(14));
        }
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync());

        // Act
        var response = await CheckoutAsync(memberId, copyId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Conflict);
        (await response.ReadProblemAsync()).Code.Should().Be("loan_limit_reached");
    }

    [Theory]
    [InlineData(CopyStatusIds.Borrowed)]
    [InlineData(CopyStatusIds.Maintenance)]
    [InlineData(CopyStatusIds.Lost)]
    public async Task Checkout_CopyNotAvailable_Returns409CopyNotAvailable(byte copyStatusId)
    {
        // Arrange
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync(), statusId: copyStatusId);
        var memberId = await Data.AddMemberAsync();

        // Act
        var response = await CheckoutAsync(memberId, copyId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Conflict);
        (await response.ReadProblemAsync()).Code.Should().Be("copy_not_available");
    }

    [Fact]
    public async Task Checkout_CopyOfSoftDeletedBook_Returns409BookDeleted()
    {
        // Arrange
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync(isDeleted: true));
        var memberId = await Data.AddMemberAsync();

        // Act
        var response = await CheckoutAsync(memberId, copyId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Conflict);
        (await response.ReadProblemAsync()).Code.Should().Be("book_deleted");
    }

    [Fact]
    public async Task Checkout_SameBookAlreadyOnLoan_Returns409SameBookOnLoan()
    {
        // Arrange
        var bookId = await Data.AddBookAsync();
        var memberId = await Data.AddMemberAsync();
        var heldCopyId = await Data.AddCopyAsync(bookId);
        await Data.AddActiveLoanAsync(memberId, heldCopyId, Today, Today.AddDays(14));
        var copyId = await Data.AddCopyAsync(bookId);

        // Act
        var response = await CheckoutAsync(memberId, copyId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Conflict);
        (await response.ReadProblemAsync()).Code.Should().Be("same_book_on_loan");
    }

    [Fact]
    public async Task Checkout_Refused_LeavesCopyAvailableAndCreatesNoLoan()
    {
        // Arrange
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync());
        var memberId = await Data.AddMemberAsync(isActive: false);

        // Act
        var response = await CheckoutAsync(memberId, copyId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Conflict);
        (await Data.GetCopyStatusAsync(copyId)).Should().Be(CopyStatusIds.Available);
        (await Data.CountLoansForCopyAsync(copyId)).Should().Be(0);
    }

    [Fact]
    public async Task Checkout_UnknownMember_Returns404()
    {
        // Arrange
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync());

        // Act
        var response = await CheckoutAsync(UnknownId, copyId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Checkout_SoftDeletedMember_Returns404()
    {
        // Arrange
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync());
        var memberId = await Data.AddMemberAsync(isDeleted: true);

        // Act
        var response = await CheckoutAsync(memberId, copyId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Checkout_UnknownCopy_Returns404()
    {
        // Arrange
        var memberId = await Data.AddMemberAsync();

        // Act
        var response = await CheckoutAsync(memberId, UnknownId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Checkout_SoftDeletedCopy_Returns404()
    {
        // Arrange
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync(), isDeleted: true);
        var memberId = await Data.AddMemberAsync();

        // Act
        var response = await CheckoutAsync(memberId, copyId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{ "bookCopyId": 1 }""")]
    [InlineData("""{ "memberId": 0, "bookCopyId": 1 }""")]
    [InlineData("""{ "memberId": 1, "bookCopyId": 0 }""")]
    [InlineData("""{ "memberId": -1, "bookCopyId": 1 }""")]
    [InlineData("""{ "memberId": 1, "bookCopyId": -1 }""")]
    [InlineData("""{ "memberId": "abc", "bookCopyId": 1 }""")]
    [InlineData("{ not json")]
    public async Task Checkout_InvalidBody_Returns400(string body)
    {
        // Arrange
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        // Act
        var response = await CreateLibrarianClient().PostAsync("/api/loans", content);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Checkout_MemberWithLoanDueToday_Returns201()
    {
        // Arrange
        var memberId = await Data.AddMemberAsync();
        var heldCopyId = await Data.AddCopyAsync(await Data.AddBookAsync());
        await Data.AddActiveLoanAsync(memberId, heldCopyId, Today.AddDays(-14), dueDate: Today);
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync());

        // Act
        var response = await CheckoutAsync(memberId, copyId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Checkout_MemberWithOnlyReturnedLoans_Returns201()
    {
        // Arrange
        var memberId = await Data.AddMemberAsync();
        for (var i = 0; i < 3; i++)
        {
            var oldCopyId = await Data.AddCopyAsync(await Data.AddBookAsync());
            await Data.AddReturnedLoanAsync(memberId, oldCopyId, Today.AddDays(-30), Today.AddDays(-16), Today.AddDays(-20));
        }
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync());

        // Act
        var response = await CheckoutAsync(memberId, copyId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Checkout_SameBookPreviouslyReturned_Returns201()
    {
        // Arrange
        var bookId = await Data.AddBookAsync();
        var memberId = await Data.AddMemberAsync();
        var oldCopyId = await Data.AddCopyAsync(bookId);
        await Data.AddReturnedLoanAsync(memberId, oldCopyId, Today.AddDays(-30), Today.AddDays(-16), Today.AddDays(-20));
        var copyId = await Data.AddCopyAsync(bookId);

        // Act
        var response = await CheckoutAsync(memberId, copyId);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Created);
    }
}
