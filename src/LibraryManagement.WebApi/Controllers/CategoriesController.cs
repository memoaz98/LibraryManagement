using LibraryManagement.Application.Dtos.Categories;
using LibraryManagement.Application.Services;
using LibraryManagement.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.WebApi.Controllers;

[ApiController]
[Route("api/categories")]
[Produces("application/json")]
[Authorize(Policy = AuthInfrastructureServiceCollectionExtensions.Policies.ReaderOrAbove)]
public class CategoriesController : ControllerBase
{
    private readonly CategoryService _categoryService;

    public CategoriesController(CategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> ListAsync(
        CancellationToken cancellationToken)
    {
        var categories = await _categoryService.ListAsync(cancellationToken);
        return Ok(categories);
    }

    [HttpGet("{id:int}", Name = "GetCategoryById")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> GetByIdAsync(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetByIdAsync(id, cancellationToken);
        return Ok(category);
    }

    [HttpPost]
    [Authorize(Policy = AuthInfrastructureServiceCollectionExtensions.Policies.LibrarianOrAdmin)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> CreateAsync(
        [FromBody] CreateCategoryDto dto,
        CancellationToken cancellationToken)
    {
        var created = await _categoryService.CreateAsync(dto, cancellationToken);
        return CreatedAtRoute("GetCategoryById", new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthInfrastructureServiceCollectionExtensions.Policies.LibrarianOrAdmin)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> UpdateAsync(
        [FromRoute] int id,
        [FromBody] UpdateCategoryDto dto,
        CancellationToken cancellationToken)
    {
        var updated = await _categoryService.UpdateAsync(id, dto, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthInfrastructureServiceCollectionExtensions.Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        await _categoryService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}