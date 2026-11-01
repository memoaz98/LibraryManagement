namespace LibraryManagement.Application.Dtos.Members;

public record MemberDto(
    long Id,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive);
