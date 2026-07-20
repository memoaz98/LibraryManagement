namespace LibraryManagement.BlazorClient.Models.Authors;

public class AuthorDto
{
    public long Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string? Biography { get; set; }

    public AuthorDto(long id, string firstName, string lastName, DateOnly? dateOfBirth = null, string? biography = null)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        DateOfBirth = dateOfBirth;
        Biography = biography;
    }
}