namespace LibraryManagement.Application.Dtos.Categories;

/// <summary>
/// Write-side DTO for POST /categories.
/// Id, IsDeleted, and CreatedAt are intentionally absent — they are
/// assigned by the database or domain factory.
/// </summary>
public record CreateCategoryDto(
    string Name,
    string? Description);