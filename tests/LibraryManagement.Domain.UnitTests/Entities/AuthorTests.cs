using FluentAssertions;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Exceptions;
using Xunit;

namespace LibraryManagement.Domain.UnitTests.Entities;

public class AuthorTests
{
    [Fact]
    public void Create_WithValidData_ReturnsAuthor()
    {
        var author = Author.Create("Gabriel", "García Márquez");

        author.FirstName.Should().Be("Gabriel");
        author.LastName.Should().Be("García Márquez");
        author.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void FullName_ComposesFirstAndLastName()
    {
        var author = Author.Create("Gabriel", "García Márquez");

        author.FullName.Should().Be("Gabriel García Márquez");
    }

    [Fact]
    public void Create_WithMissingFirstName_Throws()
    {
        var act = () => Author.Create("", "García Márquez");

        act.Should().Throw<DomainException>()
            .WithMessage("*firstName*");
    }

    [Fact]
    public void Create_WithFutureDateOfBirth_Throws()
    {
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        var act = () => Author.Create("John", "Doe", future);

        act.Should().Throw<DomainException>()
            .WithMessage("*future*");
    }

    [Fact]
    public void UpdateDateOfBirth_WithFutureDate_Throws()
    {
        var author = Author.Create("John", "Doe");
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        var act = () => author.UpdateDateOfBirth(future);

        act.Should().Throw<DomainException>();
    }
}