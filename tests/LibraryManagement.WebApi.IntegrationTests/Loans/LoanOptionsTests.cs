using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryManagement.Application.Dtos.Loans;
using LibraryManagement.Domain.Constants;
using LibraryManagement.WebApi.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace LibraryManagement.WebApi.IntegrationTests.Loans;

/// <summary>
/// The "Loan" configuration section (BR-07). Each test starts its own host
/// with different settings. Test plan IT-74 and IT-75.
/// </summary>
[Collection(ApiCollection.Name)]
public class LoanOptionsTests : IntegrationTestBase
{
    public LoanOptionsTests(LibraryApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Checkout_UsesConfiguredLoanDuration()
    {
        // Arrange
        await using var host = Factory.WithWebHostBuilder(
            builder => builder.UseSetting("Loan:DefaultLoanDays", "7"));
        var client = host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            Factory.CreateTokenFor("test-librarian", RoleIds.Names.Librarian));
        var copyId = await Data.AddCopyAsync(await Data.AddBookAsync());
        var memberId = await Data.AddMemberAsync();

        // Act
        var response = await CheckoutAsync(memberId, copyId, client);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Created);
        (await response.Content.ReadFromJsonAsync<LoanDto>())!.DueDate.Should().Be(Today.AddDays(7));
    }

    [Theory]
    [InlineData("Loan:DefaultLoanDays", "0")]
    [InlineData("Loan:DefaultLoanDays", "91")]
    [InlineData("Loan:MaxActiveLoansPerMember", "0")]
    public async Task Startup_InvalidLoanOptions_FailsFast(string key, string value)
    {
        // Arrange
        await using var host = Factory.WithWebHostBuilder(builder => builder.UseSetting(key, value));

        // Act
        var act = () => host.CreateClient();

        // Assert
        act.Should().Throw<OptionsValidationException>();
    }
}
