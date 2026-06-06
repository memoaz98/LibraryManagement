using FluentValidation;
using LibraryManagement.Application.Common.Exceptions;
using LibraryManagement.Application.Dtos.Books;
using LibraryManagement.Application.Mappers;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Exceptions;
using LibraryManagement.Domain.Interfaces;

namespace LibraryManagement.Application.Services;

public class BookService
{
    private readonly IBookRepository _bookRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUnitOfWork _uow;
    private readonly IValidator<CreateBookDto> _createValidator;
    private readonly IValidator<UpdateBookDto> _updateValidator;

    public BookService(
        IBookRepository bookRepo,
        ICategoryRepository categoryRepo,
        IUnitOfWork uow,
        IValidator<CreateBookDto> createValidator,
        IValidator<UpdateBookDto> updateValidator)
    {
        _bookRepo = bookRepo;
        _categoryRepo = categoryRepo;
        _uow = uow;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<BookDto>> ListAsync(CancellationToken cancellationToken)
    {
        var books = await _bookRepo.ListAsync(cancellationToken);
        return books.Select(BookDtoMapper.ToDto).ToList();
    }

    public async Task<BookDto> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var book = await _bookRepo.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Book), id);

        return BookDtoMapper.ToDto(book);
    }

    public async Task<BookDto> CreateAsync(CreateBookDto dto, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(dto, cancellationToken);

        var categoryExists = await _categoryRepo.GetByIdAsync(dto.CategoryId, cancellationToken);
        if (categoryExists is null)
        {
            throw new NotFoundException(nameof(Category), dto.CategoryId);
        }

        if (await _bookRepo.IsbnExistsAsync(dto.Isbn, cancellationToken))
        {
            throw new ConflictException($"A book with ISBN '{dto.Isbn}' already exists.");
        }

        var book = BookDtoMapper.ToDomainForCreate(dto);

        await _bookRepo.AddAsync(book, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return BookDtoMapper.ToDto(book);
    }

    public async Task<BookDto> UpdateAsync(long id, UpdateBookDto dto, CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(dto, cancellationToken);

        var book = await _bookRepo.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Book), id);

        if (book.CategoryId != dto.CategoryId)
        {
            var categoryExists = await _categoryRepo.GetByIdAsync(dto.CategoryId, cancellationToken);
            if (categoryExists is null)
            {
                throw new NotFoundException(nameof(Category), dto.CategoryId);
            }
            book.ChangeCategory(dto.CategoryId);
        }

        book.UpdateTitle(dto.Title);
        book.UpdateSynopsis(dto.Synopsis);

        _bookRepo.Update(book);
        await _uow.SaveChangesAsync(cancellationToken);

        return BookDtoMapper.ToDto(book);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var book = await _bookRepo.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Book), id);

        _bookRepo.Remove(book);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}