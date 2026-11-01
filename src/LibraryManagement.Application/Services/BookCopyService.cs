using FluentValidation;
using LibraryManagement.Application.Dtos.BookCopies;
using LibraryManagement.Domain.Interfaces;

namespace LibraryManagement.Application.Services;

/// <summary>
/// Book copy lookups for the checkout form.
/// </summary>
public class BookCopyService
{
    private readonly IBookCopyRepository _copyRepo;
    private readonly IBookRepository _bookRepo;
    private readonly IValidator<BookCopyQueryDto> _queryValidator;

    public BookCopyService(
        IBookCopyRepository copyRepo,
        IBookRepository bookRepo,
        IValidator<BookCopyQueryDto> queryValidator)
    {
        _copyRepo = copyRepo;
        _bookRepo = bookRepo;
        _queryValidator = queryValidator;
    }

    /// <summary>
    /// Lists the copies of a book, optionally only the available ones.
    /// </summary>
    public async Task<IReadOnlyList<BookCopyDto>> ListByBookAsync(
        long bookId,
        BookCopyQueryDto query,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }
}
