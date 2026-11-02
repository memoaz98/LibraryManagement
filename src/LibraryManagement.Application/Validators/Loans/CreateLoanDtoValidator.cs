using FluentValidation;
using LibraryManagement.Application.Dtos.Loans;

namespace LibraryManagement.Application.Validators.Loans;

public class CreateLoanDtoValidator : AbstractValidator<CreateLoanDto>
{
    public CreateLoanDtoValidator()
    {
        RuleFor(x => x.MemberId)
            .GreaterThan(0).WithMessage("MemberId must be positive.");

        RuleFor(x => x.BookCopyId)
            .GreaterThan(0).WithMessage("BookCopyId must be positive.");
    }
}
