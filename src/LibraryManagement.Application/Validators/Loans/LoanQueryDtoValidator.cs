using FluentValidation;
using LibraryManagement.Application.Dtos.Loans;

namespace LibraryManagement.Application.Validators.Loans;

public class LoanQueryDtoValidator : AbstractValidator<LoanQueryDto>
{
    public LoanQueryDtoValidator()
    {
        // Scaffolding: rules are added with the implementation.
    }
}
