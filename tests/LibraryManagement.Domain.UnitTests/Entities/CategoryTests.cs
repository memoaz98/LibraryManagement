using FluentAssertions;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Exceptions;
using Xunit;

namespace LibraryManagement.Domain.UnitTests.Entities;

public class CategoryTests
{
    [Fact]
    public void Create_WithValidData_ReturnsCategory()
    {
        var category = Category.Create("Fiction", "Literary works");

        category.Name.Should().Be("Fiction");
        category.Description.Should().Be("Literary works");
        category.IsDeleted.Should().BeFalse();
        category.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_TrimsName()
    {
        var category = Category.Create("   Fiction   ");

        category.Name.Should().Be("Fiction");
    }

    [Fact]
    public void Create_WithEmptyName_Throws()
    {
        var act = () => Category.Create("");

        act.Should().Throw<DomainException>()
            .WithMessage("*name*required*");
    }

    [Fact]
    public void Create_WithWhitespaceName_Throws()
    {
        var act = () => Category.Create("   ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithNameTooLong_Throws()
    {
        var longName = new string('a', 101);

        var act = () => Category.Create(longName);

        act.Should().Throw<DomainException>()
            .WithMessage("*100 characters*");
    }

    [Fact]
    public void Rename_WithValidName_UpdatesName()
    {
        var category = Category.Create("Fiction");

        category.Rename("Literature");

        category.Name.Should().Be("Literature");
    }

    [Fact]
    public void Rename_WithEmptyName_Throws()
    {
        var category = Category.Create("Fiction");

        var act = () => category.Rename("");

        act.Should().Throw<DomainException>();
    }
}