namespace LibraryManagement.Application.Dtos.Categories;

/// <summary>
/// Read-side DTO. Returned to the client in GET responses.
/// </summary>
public record CategoryDto(
    int Id,
    string Name,
    string? Description,
    DateTime CreatedAt);