using FluentValidation;
using LibraryManagement.Application.Dtos.Books;

namespace LibraryManagement.Application.Validators.Books;

public class CreateBookDtoValidator : AbstractValidator<CreateBookDto>
{
    public CreateBookDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(255).WithMessage("Title must not exceed 255 characters.");

        RuleFor(x => x.Isbn)
            .NotEmpty().WithMessage("ISBN is required.")
            .Matches(@"^\d{13}$").WithMessage("ISBN must be 13 numeric digits.");

        RuleFor(x => x.PublicationYear)
            .GreaterThanOrEqualTo((short)1450).WithMessage("PublicationYear must be 1450 or later.")
            .LessThanOrEqualTo((short)(DateTime.UtcNow.Year + 1))
                .WithMessage("PublicationYear cannot be in the future.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("CategoryId must be positive.");
    }
}