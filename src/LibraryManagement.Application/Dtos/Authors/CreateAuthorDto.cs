namespace LibraryManagement.Application.Dtos.Authors;

public record CreateAuthorDto(
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth = null,
    string? Biography = null);
