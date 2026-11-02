using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryManagement.Application.Dtos.BookCopies;
using LibraryManagement.Domain.Constants;
using LibraryManagement.WebApi.IntegrationTests.Fixtures;

namespace LibraryManagement.WebApi.IntegrationTests.Lookups;

/// <summary>
/// GET /api/books/{bookId}/copies. Test plan IT-67 to IT-73.
/// </summary>
[Collection(ApiCollection.Name)]
public class BookCopiesTests : IntegrationTestBase
{
    public BookCopiesTests(LibraryApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Copies_NoStatus_ReturnsAllSortedByInventoryCode()
    {
        // Arrange
        var bookId = await Data.AddBookAsync();
        await Data.AddCopyAsync(bookId, inventoryCode: "INV-C");
        await Data.AddCopyAsync(bookId, inventoryCode: "INV-A", statusId: CopyStatusIds.Borrowed);
        await Data.AddCopyAsync(bookId, inventoryCode: "INV-B", statusId: CopyStatusIds.Maintenance);
        await Data.AddCopyAsync(await Data.AddBookAsync(), inventoryCode: "INV-0");

        // Act
        var response = await CreateLibrarianClient().GetAsync($"/api/books/{bookId}/copies");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        (await ReadCodesAsync(response)).Should().Equal("INV-A", "INV-B", "INV-C");
    }

    [Fact]
    public async Task Copies_StatusAvailable_ReturnsOnlyAvailable()
    {
        // Arrange
        var bookId = await SeedMixedCopiesAsync();

        // Act
        var response = await CreateLibrarianClient().GetAsync($"/api/books/{bookId}/copies?status=available");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        (await ReadCodesAsync(response)).Should().Equal("INV-A", "INV-D");
    }

    [Theory]
    [InlineData("Available")]
    [InlineData("AVAILABLE")]
    public async Task Copies_StatusFilter_IsCaseInsensitive(string status)
    {
        // Arrange
        var bookId = await SeedMixedCopiesAsync();

        // Act
        var response = await CreateLibrarianClient().GetAsync($"/api/books/{bookId}/copies?status={status}");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        (await ReadCodesAsync(response)).Should().Equal("INV-A", "INV-D");
    }

    [Theory]
    [InlineData("borrowed")]
    [InlineData("x")]
    public async Task Copies_InvalidStatus_Returns400(string status)
    {
        // Arrange
        var bookId = await Data.AddBookAsync();

        // Act
        var response = await CreateLibrarianClient().GetAsync($"/api/books/{bookId}/copies?status={status}");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Copies_UnknownBook_Returns404()
    {
        // Act
        var response = await CreateLibrarianClient().GetAsync("/api/books/999999/copies");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Copies_SoftDeletedBook_Returns404()
    {
        // Arrange
        var bookId = await Data.AddBookAsync(isDeleted: true);
        await Data.AddCopyAsync(bookId);

        // Act
        var response = await CreateLibrarianClient().GetAsync($"/api/books/{bookId}/copies");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Copies_ExcludesSoftDeletedCopies()
    {
        // Arrange
        var bookId = await Data.AddBookAsync();
        await Data.AddCopyAsync(bookId, inventoryCode: "INV-A");
        await Data.AddCopyAsync(bookId, inventoryCode: "INV-B", isDeleted: true);

        // Act
        var response = await CreateLibrarianClient().GetAsync($"/api/books/{bookId}/copies");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        (await ReadCodesAsync(response)).Should().Equal("INV-A");
    }

    /// <summary>A book with two available copies (INV-A, INV-D) and two that are not.</summary>
    private async Task<long> SeedMixedCopiesAsync()
    {
        var bookId = await Data.AddBookAsync();
        await Data.AddCopyAsync(bookId, inventoryCode: "INV-D");
        await Data.AddCopyAsync(bookId, inventoryCode: "INV-B", statusId: CopyStatusIds.Borrowed);
        await Data.AddCopyAsync(bookId, inventoryCode: "INV-A");
        await Data.AddCopyAsync(bookId, inventoryCode: "INV-C", statusId: CopyStatusIds.Lost);
        return bookId;
    }

    private static async Task<string[]> ReadCodesAsync(HttpResponseMessage response)
    {
        var copies = await response.Content.ReadFromJsonAsync<List<BookCopyDto>>();
        return copies!.Select(copy => copy.InventoryCode).ToArray();
    }
}
