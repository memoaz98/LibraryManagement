using LibraryManagement.Application.Dtos.Authors;
using LibraryManagement.Application.Services;
using LibraryManagement.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.WebApi.Controllers;

[ApiController]
[Route("api/authors")]
[Produces("application/json")]
[Authorize(Policy = AuthInfrastructureServiceCollectionExtensions.Policies.ReaderOrAbove)]
public class AuthorsController : ControllerBase
{
    private readonly AuthorService _authorService;

    public AuthorsController(AuthorService authorService)
    {
        _authorService = authorService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AuthorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AuthorDto>>> ListAsync(
        CancellationToken cancellationToken)
    {
        var authors = await _authorService.ListAsync(cancellationToken);
        return Ok(authors);
    }

    [HttpPost]
    //[Authorize(Policy = AuthInfrastructureServiceCollectionExtensions.Policies.LibrarianOrAdmin)]
    [ProducesResponseType(typeof(AuthorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthorDto>> CreateAsync(
        [FromBody] CreateAuthorDto dto,
        CancellationToken cancellationToken)
    {
        var created = await _authorService.CreateAsync(dto, cancellationToken);
        //return CreatedAtRoute("GetAuthorById", new { id = created.Id }, created);
        return Ok(created);
    }

    [HttpPut("{id:long}")]
    //[Authorize(Policy = AuthInfrastructureServiceCollectionExtensions.Policies.LibrarianOrAdmin)]
    [ProducesResponseType(typeof(UpdateAuthorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateAuthorDto>> UpdateAsync(
        [FromRoute] long id, 
        [FromBody] UpdateAuthorDto dto, 
        CancellationToken cancellationToken)
    {
        var updated = await _authorService.UpdateAuthor(id, dto, cancellationToken);
        return Ok(updated);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(AuthorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthorDto>> GetByIdAsync(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        var author = await _authorService.GetByIdAsync(id, cancellationToken);
        return Ok(author);
    }



}