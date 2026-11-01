using LibraryManagement.Application.Dtos.Loans;
using LibraryManagement.Application.Services;
using LibraryManagement.Infrastructure.DependencyInjection;
using LibraryManagement.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.WebApi.Controllers;

[ApiController]
[Route("api/loans")]
[Produces("application/json")]
[Authorize(Policy = AuthInfrastructureServiceCollectionExtensions.Policies.ReaderOrAbove)]
public class LoansController : ControllerBase
{
    private readonly LoanService _loanService;

    public LoansController(LoanService loanService)
    {
        _loanService = loanService;
    }

    [HttpGet]
    [Authorize(Policy = AuthInfrastructureServiceCollectionExtensions.Policies.LibrarianOrAdmin)]
    [ProducesResponseType(typeof(IReadOnlyList<LoanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<LoanDto>>> ListAsync(
        [FromQuery] long? memberId,
        [FromQuery] long? bookCopyId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var loans = await _loanService.ListAsync(
            new LoanQueryDto(memberId, bookCopyId, status),
            cancellationToken);
        return Ok(loans);
    }

    [HttpGet("{id:long}", Name = "GetLoanById")]
    [ProducesResponseType(typeof(LoanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoanDto>> GetByIdAsync(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        var loan = await _loanService.GetByIdAsync(
            id,
            User.GetUserId(),
            User.IsLibrarianOrAdmin(),
            cancellationToken);
        return Ok(loan);
    }

    [HttpPost]
    [Authorize(Policy = AuthInfrastructureServiceCollectionExtensions.Policies.LibrarianOrAdmin)]
    [ProducesResponseType(typeof(LoanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoanDto>> CheckoutAsync(
        [FromBody] CreateLoanDto dto,
        CancellationToken cancellationToken)
    {
        var created = await _loanService.CheckoutAsync(dto, cancellationToken);
        return CreatedAtRoute("GetLoanById", new { id = created.Id }, created);
    }

    [HttpPost("{id:long}/return")]
    [Authorize(Policy = AuthInfrastructureServiceCollectionExtensions.Policies.LibrarianOrAdmin)]
    [ProducesResponseType(typeof(LoanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoanDto>> ReturnAsync(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        var returned = await _loanService.ReturnAsync(id, cancellationToken);
        return Ok(returned);
    }
}
