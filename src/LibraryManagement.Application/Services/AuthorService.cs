using FluentValidation;
using LibraryManagement.Application.Common.Exceptions;
using LibraryManagement.Application.Dtos.Authors;
using LibraryManagement.Application.Mappers;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Exceptions;
using LibraryManagement.Domain.Interfaces;

namespace LibraryManagement.Application.Services;

public class AuthorService
{
    private readonly IAuthorRepository _authorRepo;
    private readonly IUnitOfWork _uow;
    private readonly IValidator<CreateAuthorDto> _createValidator;
    private readonly IValidator<UpdateAuthorDto> _updateValidator;

    public AuthorService(
        IAuthorRepository authorRepo,
        IUnitOfWork uow,
        IValidator<CreateAuthorDto> createValidator,
        IValidator<UpdateAuthorDto> updateValidator)
    {
        _authorRepo = authorRepo;
        _uow = uow;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<AuthorDto> CreateAsync(CreateAuthorDto dto, CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(dto, cancellationToken);

        Author author = AuthorsDtoMapper.ToDomainAuthorCreate(dto);
        await _authorRepo.AddAsync(author, cancellationToken);

        var id = await _uow.SaveChangesAsync(cancellationToken);

        return AuthorsDtoMapper.AuthorToDto(author);
            
    }
    
    public async Task<IReadOnlyList<AuthorDto>> ListAsync(CancellationToken cancellationToken)
    {
        var authors = await _authorRepo.ListAsync(cancellationToken);
        return authors.Select(a => AuthorsDtoMapper.AuthorToDto(a)).ToList();
    }

    public async Task<AuthorDto> UpdateAuthor(long id, UpdateAuthorDto dto, CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(dto, cancellationToken);
        
        var author = await _authorRepo.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Author), id);

        author.Rename(dto.FirstName, dto.LastName);
        author.UpdateDateOfBirth(dto.DateOfBirth);
        author.UpdateBiography(dto.Biography);

        _authorRepo.Update(author);

        await _uow.SaveChangesAsync(cancellationToken);

        return AuthorsDtoMapper.AuthorToDto(author);

    }

    public async Task<AuthorDto> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        Author author = await _authorRepo.GetByIdAsync(id, cancellationToken);
        return AuthorsDtoMapper.AuthorToDto(author);
    }

}