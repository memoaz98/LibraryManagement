using FluentValidation;
using LibraryManagement.Application.Dtos.BookCopies;

namespace LibraryManagement.Application.Validators.BookCopies;

public class BookCopyQueryDtoValidator : AbstractValidator<BookCopyQueryDto>
{
    public BookCopyQueryDtoValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => string.IsNullOrWhiteSpace(status)
                || string.Equals(status.Trim(), "available", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Status must be 'available'.");
    }
}
