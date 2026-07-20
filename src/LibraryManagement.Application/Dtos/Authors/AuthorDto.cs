namespace LibraryManagement.Application.Dtos.Authors;

public record AuthorDto(
    long Id, 
    string FirstName, 
    string LastName, 
    DateOnly? DateOfBirth
    );