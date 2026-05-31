using FluentAssertions;
using LibraryManagement.Domain.Constants;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Exceptions;
using Xunit;

namespace LibraryManagement.Domain.UnitTests.Entities;

public class LoanTests
{
    private static Loan NewActiveLoan(int days = 14) =>
        Loan.Create(memberId: 1, bookCopyId: 1, loanDurationDays: days);

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
        var act = () => Loan.Create(memberId: 1, bookCopyId: 1, loanDurationDays: 0);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithExcessiveDuration_Throws()
    {
        var act = () => Loan.Create(memberId: 1, bookCopyId: 1, loanDurationDays: 91);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Return_FromActive_ClosesLoan()
    {
        var loan = NewActiveLoan();

        loan.Return();

        loan.StatusId.Should().Be(LoanStatusIds.Returned);
        loan.ReturnDate.Should().NotBeNull();
    }

    [Fact]
    public void Return_FromAlreadyReturned_Throws()
    {
        var loan = NewActiveLoan();
        loan.Return();

        var act = () => loan.Return();

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
        loan.Return();

        var act = () => loan.MarkAsLost();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void IsCurrentlyOverdue_WithFutureDueDate_IsFalse()
    {
        var loan = NewActiveLoan(days: 14);

        loan.IsCurrentlyOverdue.Should().BeFalse();
    }
}