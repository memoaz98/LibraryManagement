using FluentAssertions;
using LibraryManagement.Domain.Constants;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Exceptions;
using Xunit;

namespace LibraryManagement.Domain.UnitTests.Entities;

public class BookCopyTests
{
    private static BookCopy NewCopy() =>
        BookCopy.Create(bookId: 1, inventoryCode: "LIB-001", acquisitionDate: DateOnly.FromDateTime(DateTime.UtcNow));

    [Fact]
    public void Create_StartsAvailable()
    {
        var copy = NewCopy();

        copy.StatusId.Should().Be(CopyStatusIds.Available);
    }

    [Fact]
    public void Create_WithFutureAcquisitionDate_Throws()
    {
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        var act = () => BookCopy.Create(1, "LIB-001", future);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkAsBorrowed_FromAvailable_Succeeds()
    {
        var copy = NewCopy();

        copy.MarkAsBorrowed();

        copy.StatusId.Should().Be(CopyStatusIds.Borrowed);
    }

    [Fact]
    public void MarkAsBorrowed_FromBorrowed_Throws()
    {
        var copy = NewCopy();
        copy.MarkAsBorrowed();

        var act = () => copy.MarkAsBorrowed();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkAsReturned_FromBorrowed_Succeeds()
    {
        var copy = NewCopy();
        copy.MarkAsBorrowed();

        copy.MarkAsReturned();

        copy.StatusId.Should().Be(CopyStatusIds.Available);
    }

    [Fact]
    public void MarkAsLost_FromAvailable_Throws()
    {
        var copy = NewCopy();

        var act = () => copy.MarkAsLost();

        act.Should().Throw<DomainException>()
            .WithMessage("*Borrowed or Maintenance*");
    }

    [Fact]
    public void MarkAsLost_FromBorrowed_Succeeds()
    {
        var copy = NewCopy();
        copy.MarkAsBorrowed();

        copy.MarkAsLost();

        copy.StatusId.Should().Be(CopyStatusIds.Lost);
    }
}