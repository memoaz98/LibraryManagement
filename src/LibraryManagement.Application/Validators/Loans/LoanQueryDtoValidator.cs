using FluentValidation;
using LibraryManagement.Application.Dtos.Loans;
using LibraryManagement.Application.Loans;

namespace LibraryManagement.Application.Validators.Loans;

public class LoanQueryDtoValidator : AbstractValidator<LoanQueryDto>
{
    public LoanQueryDtoValidator()
    {
        RuleFor(x => x.MemberId)
            .GreaterThan(0).When(x => x.MemberId.HasValue)
            .WithMessage("MemberId must be positive.");

        RuleFor(x => x.BookCopyId)
            .GreaterThan(0).When(x => x.BookCopyId.HasValue)
            .WithMessage("BookCopyId must be positive.");

        RuleFor(x => x.Status)
            .Must(status => LoanStatusFilterParser.TryParse(status, out _))
            .WithMessage("Status must be 'active' or 'returned'.");
    }
}
