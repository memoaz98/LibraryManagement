using LibraryManagement.Application.Dtos.Loans;
using LibraryManagement.Application.Dtos.Members;
using LibraryManagement.Application.Services;
using LibraryManagement.Infrastructure.DependencyInjection;
using LibraryManagement.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.WebApi.Controllers;

[ApiController]
[Route("api/members")]
[Produces("application/json")]
[Authorize(Policy = AuthInfrastructureServiceCollectionExtensions.Policies.ReaderOrAbove)]
public class MembersController : ControllerBase
{
    private readonly MemberService _memberService;
    private readonly LoanService _loanService;

    public MembersController(MemberService memberService, LoanService loanService)
    {
        _memberService = memberService;
        _loanService = loanService;
    }

    [HttpGet]
    [Authorize(Policy = AuthInfrastructureServiceCollectionExtensions.Policies.LibrarianOrAdmin)]
    [ProducesResponseType(typeof(IReadOnlyList<MemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<MemberDto>>> SearchAsync(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var members = await _memberService.SearchAsync(search, cancellationToken);
        return Ok(members);
    }

    [HttpGet("me/loans")]
    [ProducesResponseType(typeof(IReadOnlyList<LoanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<LoanDto>>> ListMyLoansAsync(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var loans = await _loanService.ListForUserAsync(User.GetUserId(), status, cancellationToken);
        return Ok(loans);
    }
}
