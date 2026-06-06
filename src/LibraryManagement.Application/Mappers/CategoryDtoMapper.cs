using LibraryManagement.Application.Dtos.Categories;
using LibraryManagement.Domain.Entities;

namespace LibraryManagement.Application.Mappers;

/// <summary>
/// Translates between the <see cref="Category"/> domain entity and its
/// data transfer objects.
/// </summary>
/// <remarks>
/// Domain → DTO is straightforward (read projection). DTO → Domain only
/// applies when *creating* (via <see cref="Category.Create"/>). For updates,
/// the service mutates the existing domain entity through its methods
/// (<see cref="Category.Rename"/>, <see cref="Category.UpdateDescription"/>)
/// — we never reconstruct the entity from an UpdateDto.
/// </remarks>
internal static class CategoryDtoMapper
{
    public static CategoryDto ToDto(Category domain)
    {
        return new CategoryDto(
            Id: domain.Id,
            Name: domain.Name,
            Description: domain.Description,
            CreatedAt: domain.CreatedAt);
    }

    public static Category ToDomainForCreate(CreateCategoryDto dto)
    {
        return Category.Create(dto.Name, dto.Description);
    }
}