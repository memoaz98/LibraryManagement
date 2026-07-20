using FluentValidation;
using LibraryManagement.Application.Dtos.Authors;

namespace LibraryManagement.Application.Validators.Authors;

public class CreateAuthorDtoValidator : AbstractValidator<CreateAuthorDto>
{
    public CreateAuthorDtoValidator()
    {
       RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");
       RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");
        RuleFor(x => x.DateOfBirth)
            .Must(date => date == null || date.Value.Year >= 1000)
            .WithMessage("Date of birth cannot be before year 1000.")
            .Must(date => date == null || date.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth cannot be in the future.");
        RuleFor(x => x.Biography)
            .MaximumLength(2000).WithMessage("Biography must not exceed 2000 characters.");
    }

}