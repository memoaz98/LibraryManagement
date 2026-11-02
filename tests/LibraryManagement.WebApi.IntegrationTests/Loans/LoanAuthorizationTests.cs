using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryManagement.WebApi.IntegrationTests.Fixtures;

namespace LibraryManagement.WebApi.IntegrationTests.Loans;

/// <summary>
/// Authentication and policies on every endpoint of the feature. Test plan
/// IT-36 and IT-37.
/// </summary>
[Collection(ApiCollection.Name)]
public class LoanAuthorizationTests : IntegrationTestBase
{
    public LoanAuthorizationTests(LibraryApiFactory factory) : base(factory)
    {
    }

    [Theory]
    [InlineData("POST", "/api/loans")]
    [InlineData("POST", "/api/loans/1/return")]
    [InlineData("GET", "/api/loans/1")]
    [InlineData("GET", "/api/loans")]
    [InlineData("GET", "/api/members/me/loans")]
    [InlineData("GET", "/api/members")]
    [InlineData("GET", "/api/books/1/copies")]
    public async Task Endpoint_WithoutToken_Returns401(string method, string url)
    {
        // Act
        var response = await Factory.CreateClient().SendAsync(CreateRequest(method, url));

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("POST", "/api/loans")]
    [InlineData("POST", "/api/loans/1/return")]
    [InlineData("GET", "/api/loans")]
    [InlineData("GET", "/api/members")]
    [InlineData("GET", "/api/books/1/copies")]
    public async Task StaffOnlyEndpoint_AsReader_Returns403(string method, string url)
    {
        // Act
        var response = await CreateReaderClient("test-reader").SendAsync(CreateRequest(method, url));

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Forbidden);
    }

    private static HttpRequestMessage CreateRequest(string method, string url)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method == "POST")
        {
            request.Content = JsonContent.Create(new { memberId = 1, bookCopyId = 1 });
        }
        return request;
    }
}
