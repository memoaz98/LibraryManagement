using FluentAssertions;
using LibraryManagement.Domain.Constants;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Exceptions;
using Xunit;

namespace LibraryManagement.Domain.UnitTests.Entities;

public class LoanTests
{
    private static readonly DateOnly Today = new(2026, 1, 15);

    private static Loan NewActiveLoan(int days = 14) =>
        Loan.Create(memberId: 1, bookCopyId: 1, loanDurationDays: days, today: Today);

    [Fact]
    public void Create_StartsActive()
    {
        var loan = NewActiveLoan();

        loan.StatusId.Should().Be(LoanStatusIds.Active);
        loan.ReturnDate.Should().BeNull();
    }

    [Fact]
    public void Create_AssignsDueDateBasedOnDuration()
    {
        var loan = NewActiveLoan(days: 14);

        loan.DueDate.Should().Be(loan.LoanDate.AddDays(14));
    }

    [Fact]
    public void Create_WithZeroDuration_Throws()
    {
        var act = () => Loan.Create(memberId: 1, bookCopyId: 1, loanDurationDays: 0, today: Today);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithExcessiveDuration_Throws()
    {
        var act = () => Loan.Create(memberId: 1, bookCopyId: 1, loanDurationDays: 91, today: Today);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Return_FromActive_ClosesLoan()
    {
        var loan = NewActiveLoan();

        loan.Return(Today);

        loan.StatusId.Should().Be(LoanStatusIds.Returned);
        loan.ReturnDate.Should().NotBeNull();
    }

    [Fact]
    public void Return_FromAlreadyReturned_Throws()
    {
        var loan = NewActiveLoan();
        loan.Return(Today);

        var act = () => loan.Return(Today);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkAsLost_FromActive_Succeeds()
    {
        var loan = NewActiveLoan();

        loan.MarkAsLost();

        loan.StatusId.Should().Be(LoanStatusIds.Lost);
        loan.ReturnDate.Should().BeNull();
    }

    [Fact]
    public void MarkAsLost_FromReturned_Throws()
    {
        var loan = NewActiveLoan();
        loan.Return(Today);

        var act = () => loan.MarkAsLost();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void IsOverdueOn_WithFutureDueDate_IsFalse()
    {
        var loan = NewActiveLoan(days: 14);

        loan.IsOverdueOn(Today).Should().BeFalse();
    }

    [Fact]
    public void IsOverdueOn_OnDueDate_IsFalse()
    {
        var loan = NewActiveLoan();

        loan.IsOverdueOn(loan.DueDate).Should().BeFalse();
    }

    [Fact]
    public void IsOverdueOn_DayAfterDueDate_IsTrue()
    {
        var loan = NewActiveLoan();

        loan.IsOverdueOn(loan.DueDate.AddDays(1)).Should().BeTrue();
    }

    [Fact]
    public void IsOverdueOn_ReturnedLoan_IsFalse()
    {
        var loan = NewActiveLoan();
        loan.Return(Today);

        loan.IsOverdueOn(loan.DueDate.AddDays(30)).Should().BeFalse();
    }

    [Fact]
    public void IsOverdueOn_LostLoan_IsFalse()
    {
        var loan = NewActiveLoan();
        loan.MarkAsLost();

        loan.IsOverdueOn(loan.DueDate.AddDays(30)).Should().BeFalse();
    }

    [Fact]
    public void Return_SetsReturnDateToGivenDay()
    {
        var loan = NewActiveLoan();
        var returnDay = Today.AddDays(3);

        loan.Return(returnDay);

        loan.ReturnDate.Should().Be(returnDay);
    }

    [Fact]
    public void Return_BeforeLoanDate_Throws()
    {
        var loan = NewActiveLoan();

        var act = () => loan.Return(Today.AddDays(-1));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Return_FromLost_Throws()
    {
        var loan = NewActiveLoan();
        loan.MarkAsLost();

        var act = () => loan.Return(Today);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Return_FromOverdue_Succeeds()
    {
        var loan = Loan.Reconstitute(
            id: 1,
            bookCopyId: 1,
            memberId: 1,
            statusId: LoanStatusIds.Overdue,
            loanDate: Today.AddDays(-20),
            dueDate: Today.AddDays(-6),
            returnDate: null,
            createdAt: DateTime.UtcNow,
            isDeleted: false);

        loan.Return(Today);

        loan.StatusId.Should().Be(LoanStatusIds.Returned);
    }

    [Fact]
    public void Return_AfterDueDate_Succeeds()
    {
        var loan = NewActiveLoan();

        loan.Return(loan.DueDate.AddDays(10));

        loan.StatusId.Should().Be(LoanStatusIds.Returned);
    }

    [Fact]
    public void Create_SetsLoanDateToToday()
    {
        var loan = NewActiveLoan();

        loan.LoanDate.Should().Be(Today);
    }

    [Fact]
    public void Create_WithMinimumDuration_IsDueTomorrow()
    {
        var loan = NewActiveLoan(days: 1);

        loan.DueDate.Should().Be(Today.AddDays(1));
    }

    [Fact]
    public void Create_WithMaximumDuration_IsDueIn90Days()
    {
        var loan = NewActiveLoan(days: 90);

        loan.DueDate.Should().Be(Today.AddDays(90));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveMemberId_Throws(long memberId)
    {
        var act = () => Loan.Create(memberId, bookCopyId: 1, loanDurationDays: 14, today: Today);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveBookCopyId_Throws(long bookCopyId)
    {
        var act = () => Loan.Create(memberId: 1, bookCopyId, loanDurationDays: 14, today: Today);

        act.Should().Throw<DomainException>();
    }
}