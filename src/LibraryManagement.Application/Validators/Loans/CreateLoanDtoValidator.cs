using FluentValidation;
using LibraryManagement.Application.Dtos.Loans;

namespace LibraryManagement.Application.Validators.Loans;

public class CreateLoanDtoValidator : AbstractValidator<CreateLoanDto>
{
    public CreateLoanDtoValidator()
    {
        // Scaffolding: rules are added with the implementation.
    }
}
