using FluentValidation;
using LibraryManagement.Application.Dtos.BookCopies;

namespace LibraryManagement.Application.Validators.BookCopies;

public class BookCopyQueryDtoValidator : AbstractValidator<BookCopyQueryDto>
{
    public BookCopyQueryDtoValidator()
    {
        // Scaffolding: rules are added with the implementation.
    }
}
