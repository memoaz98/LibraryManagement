using FluentValidation;
using LibraryManagement.Application.Common.Exceptions;
using LibraryManagement.Application.Dtos.Categories;
using LibraryManagement.Application.Mappers;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Exceptions;
using LibraryManagement.Domain.Interfaces;

namespace LibraryManagement.Application.Services;

/// <summary>
/// Coordinates the Category use cases: input validation, business
/// checks (uniqueness), domain mutation, persistence, and DTO mapping.
/// </summary>
public class CategoryService
{
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUnitOfWork _uow;
    private readonly IValidator<CreateCategoryDto> _createValidator;
    private readonly IValidator<UpdateCategoryDto> _updateValidator;

    public CategoryService(
        ICategoryRepository categoryRepo,
        IUnitOfWork uow,
        IValidator<CreateCategoryDto> createValidator,
        IValidator<UpdateCategoryDto> updateValidator)
    {
        _categoryRepo = categoryRepo;
        _uow = uow;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken cancellationToken)
    {
        var categories = await _categoryRepo.ListAsync(cancellationToken);
        return categories.Select(CategoryDtoMapper.ToDto).ToList();
    }

    public async Task<CategoryDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var category = await _categoryRepo.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), id);

        return CategoryDtoMapper.ToDto(category);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(dto, cancellationToken);

        if (await _categoryRepo.NameExistsAsync(dto.Name, cancellationToken))
        {
            throw new ConflictException($"A category named '{dto.Name}' already exists.");
        }

        var category = CategoryDtoMapper.ToDomainForCreate(dto);

        await _categoryRepo.AddAsync(category, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return CategoryDtoMapper.ToDto(category);
    }

    public async Task<CategoryDto> UpdateAsync(int id, UpdateCategoryDto dto, CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(dto, cancellationToken);

        var category = await _categoryRepo.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), id);

        if (!string.Equals(category.Name, dto.Name, StringComparison.Ordinal))
        {
            if (await _categoryRepo.NameExistsAsync(dto.Name, cancellationToken))
            {
                throw new ConflictException($"A category named '{dto.Name}' already exists.");
            }
            category.Rename(dto.Name);
        }

        category.UpdateDescription(dto.Description);

        _categoryRepo.Update(category);
        await _uow.SaveChangesAsync(cancellationToken);

        return CategoryDtoMapper.ToDto(category);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var category = await _categoryRepo.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), id);

        _categoryRepo.Remove(category);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}