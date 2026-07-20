namespace LibraryManagement.Application.Dtos.Authors;

public record UpdateAuthorDto(
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth = null,
    string? Biography = null
);  
