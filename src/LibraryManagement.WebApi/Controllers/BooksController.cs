using LibraryManagement.Application.Dtos.Books;
using LibraryManagement.Application.Services;
using LibraryManagement.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.WebApi.Controllers;

[ApiController]
[Route("api/books")]
[Produces("application/json")]
[Authorize(Policy = AuthInfrastructureServiceCollectionExtensions.Policies.ReaderOrAbove)]
public class BooksController : ControllerBase
{
    private readonly BookService _bookService;

    public BooksController(BookService bookService)
    {
        _bookService = bookService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BookDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookDto>>> ListAsync(
        CancellationToken cancellationToken)
    {
        var books = await _bookService.ListAsync(cancellationToken);
        return Ok(books);
    }

    [HttpGet("{id:long}", Name = "GetBookById")]
    [ProducesResponseType(typeof(BookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookDto>> GetByIdAsync(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        var book = await _bookService.GetByIdAsync(id, cancellationToken);
        return Ok(book);
    }

    [HttpPost]
    [Authorize(Policy = AuthInfrastructureServiceCollectionExtensions.Policies.LibrarianOrAdmin)]
    [ProducesResponseType(typeof(BookDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookDto>> CreateAsync(
        [FromBody] CreateBookDto dto,
        CancellationToken cancellationToken)
    {
        var created = await _bookService.CreateAsync(dto, cancellationToken);
        return CreatedAtRoute("GetBookById", new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = AuthInfrastructureServiceCollectionExtensions.Policies.LibrarianOrAdmin)]
    [ProducesResponseType(typeof(BookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookDto>> UpdateAsync(
        [FromRoute] long id,
        [FromBody] UpdateBookDto dto,
        CancellationToken cancellationToken)
    {
        var updated = await _bookService.UpdateAsync(id, dto, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = AuthInfrastructureServiceCollectionExtensions.Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        await _bookService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}