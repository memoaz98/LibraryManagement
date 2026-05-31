using FluentAssertions;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Exceptions;
using Xunit;

namespace LibraryManagement.Domain.UnitTests.Entities;

public class BookTests
{
    // Real ISBN-13s with valid checksums (verified)
    private const string ValidIsbn1 = "9780306406157";
    private const string ValidIsbn2 = "9780451524935";

    [Fact]
    public void Create_WithValidData_ReturnsBook()
    {
        var book = Book.Create("1984", ValidIsbn2, 1949, categoryId: 1);

        book.Title.Should().Be("1984");
        book.Isbn.Should().Be(ValidIsbn2);
        book.PublicationYear.Should().Be(1949);
        book.CategoryId.Should().Be(1);
    }

    [Fact]
    public void Create_WithInvalidIsbnLength_Throws()
    {
        var act = () => Book.Create("Test", "123", 2020, categoryId: 1);

        act.Should().Throw<DomainException>()
            .WithMessage("*13 digits*");
    }

    [Fact]
    public void Create_WithNonNumericIsbn_Throws()
    {
        var act = () => Book.Create("Test", "ABC0306406157", 2020, categoryId: 1);

        act.Should().Throw<DomainException>()
            .WithMessage("*13 digits*");
    }

    [Fact]
    public void Create_WithInvalidIsbnChecksum_Throws()
    {
        // Last digit changed from 7 to 8 — checksum no longer matches
        var act = () => Book.Create("Test", "9780306406158", 2020, categoryId: 1);

        act.Should().Throw<DomainException>()
            .WithMessage("*checksum*");
    }

    [Fact]
    public void Create_WithYearBeforeGutenberg_Throws()
    {
        var act = () => Book.Create("Ancient", ValidIsbn1, 1400, categoryId: 1);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithCategoryIdZero_Throws()
    {
        var act = () => Book.Create("Test", ValidIsbn1, 2020, categoryId: 0);

        act.Should().Throw<DomainException>()
            .WithMessage("*positive*");
    }

    [Fact]
    public void UpdateTitle_ChangesTitle()
    {
        var book = Book.Create("1984", ValidIsbn2, 1949, categoryId: 1);

        book.UpdateTitle("Nineteen Eighty-Four");

        book.Title.Should().Be("Nineteen Eighty-Four");
    }

    [Fact]
    public void ChangeCategory_UpdatesCategoryId()
    {
        var book = Book.Create("1984", ValidIsbn2, 1949, categoryId: 1);

        book.ChangeCategory(5);

        book.CategoryId.Should().Be(5);
    }
}