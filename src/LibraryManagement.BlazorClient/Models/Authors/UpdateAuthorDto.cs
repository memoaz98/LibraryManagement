namespace LibraryManagement.BlazorClient.Models.Authors;

public class UpdateAuthorDto
{
    public long Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string? Biography { get; set; }

    public UpdateAuthorDto(string firstName, string lastName, DateOnly? dateOfBirth = null, string? biography = null)
    {
        FirstName = firstName;
        LastName = lastName;
        DateOfBirth = dateOfBirth;
        Biography = biography;
    }
}