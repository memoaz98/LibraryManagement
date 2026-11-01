using LibraryManagement.Application.Dtos.BookCopies;
using LibraryManagement.Application.Services;
using LibraryManagement.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.WebApi.Controllers;

[ApiController]
[Route("api/books/{bookId:long}/copies")]
[Produces("application/json")]
[Authorize(Policy = AuthInfrastructureServiceCollectionExtensions.Policies.ReaderOrAbove)]
public class BookCopiesController : ControllerBase
{
    private readonly BookCopyService _bookCopyService;

    public BookCopiesController(BookCopyService bookCopyService)
    {
        _bookCopyService = bookCopyService;
    }

    [HttpGet]
    [Authorize(Policy = AuthInfrastructureServiceCollectionExtensions.Policies.LibrarianOrAdmin)]
    [ProducesResponseType(typeof(IReadOnlyList<BookCopyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<BookCopyDto>>> ListAsync(
        [FromRoute] long bookId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var copies = await _bookCopyService.ListByBookAsync(
            bookId,
            new BookCopyQueryDto(status),
            cancellationToken);
        return Ok(copies);
    }
}
