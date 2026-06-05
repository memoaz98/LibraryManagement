using LibraryManagement.Domain.Entities;
using LibraryManagement.Infrastructure.Persistence.DataModels;

namespace LibraryManagement.Infrastructure.Persistence.Mappers;

/// <summary>
/// Translates between the <see cref="Category"/> domain entity and the
/// <see cref="CategoryDataModel"/> persistence model.
/// </summary>
internal static class CategoryMapper
{
    public static Category ToDomain(CategoryDataModel dataModel)
    {
        return Category.Reconstitute(
            id: dataModel.Id,
            name: dataModel.Name,
            description: dataModel.Description,
            createdAt: dataModel.CreatedAt,
            isDeleted: dataModel.IsDeleted);
    }

    public static CategoryDataModel ToDataModel(Category domain)
    {
        return new CategoryDataModel
        {
            Id = domain.Id,
            Name = domain.Name,
            Description = domain.Description,
            CreatedAt = domain.CreatedAt,
            IsDeleted = domain.IsDeleted
        };
    }
}