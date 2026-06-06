namespace LibraryManagement.Application.Dtos.Categories;

/// <summary>
/// Write-side DTO for PUT /categories/{id}.
/// Id arrives in the URL, not in the body.
/// </summary>
public record UpdateCategoryDto(
    string Name,
    string? Description);